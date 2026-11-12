using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DTZero.Internal;

namespace DTZero.AspNetCore;

public static class WebApplicationExtensions
{
	/// <summary>
	/// Configures the DTZero endpoint for handling remote delegate requests.
	/// Adds the X-DTZero-Format response header to communicate the serialization format.
	/// Automatically extracts correlation ID from request headers.
	/// Supports cancellation via client disconnect and server shutdown.
	/// </summary>
	public static WebApplication UseDTZero(this WebApplication app)
	{
		ArgumentNullException.ThrowIfNull(app);

		// Configure ambient logging with the application's logger factory
		var loggerFactory = app.Services.GetService<ILoggerFactory>();
		if (loggerFactory != null)
		{
			DTZeroLogging.SetLoggerFactory(loggerFactory);
		}

		// Get the application lifetime for graceful shutdown support
		var hostLifetime = app.Services.GetService<IHostApplicationLifetime>();

		// Add middleware for correlation ID extraction
		app.Use(async (context, next) =>
		{
			// Get the scoped correlation context for this request
			var correlationContext = context.RequestServices.GetRequiredService<ICorrelationContext>();

			// Extract correlation ID from request headers or generate a new one
			if (context.Request.Headers.TryGetValue(CorrelationContextImpl.HeaderName, out var headerValue)
				&& !string.IsNullOrEmpty(headerValue.FirstOrDefault()))
			{
				correlationContext.CorrelationId = headerValue.FirstOrDefault();
			}
			else
			{
				correlationContext.CorrelationId = CorrelationContextImpl.GenerateCorrelationId();
			}

			// Add correlation ID to response headers for client-side tracing
			context.Response.Headers[CorrelationContextImpl.HeaderName] = correlationContext.CorrelationId;
			await next();
		});

		app.MapPost("/api/dtzero", async (HttpContext httpContext, RemoteRequestDto request) =>
		{
			var handleRemoteDelegateRequest = httpContext.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
			var serializationOptions = httpContext.RequestServices.GetService<DTZeroSerializationOptions>();

			// Add the serialization format header to the response
			if (serializationOptions != null)
			{
				httpContext.Response.Headers[DTZeroSerializationOptions.FormatHeaderName] = serializationOptions.FormatHeaderValue;
			}

			// Create a linked cancellation token that fires on:
			// 1. Client disconnect (HttpContext.RequestAborted)
			// 2. Server graceful shutdown (ApplicationStopping)
			CancellationToken cancellationToken;
			CancellationTokenSource? linkedCts = null;

			if (hostLifetime != null)
			{
				linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
					httpContext.RequestAborted,
					hostLifetime.ApplicationStopping);
				cancellationToken = linkedCts.Token;
			}
			else
			{
				cancellationToken = httpContext.RequestAborted;
			}

			try
			{
				return await handleRemoteDelegateRequest(request, cancellationToken);
			}
			finally
			{
				linkedCts?.Dispose();
			}
		})
		.AddEndpointFilter(async (context, next) =>
		{
			// A cancellation caused by the client leaving or the server stopping is not a
			// server failure (#15). Escaping, it would reach the pipeline as a 500 that
			// request logging writes at Error with a stack trace. Any other
			// OperationCanceledException is the operation's own, and still escapes.
			try
			{
				return await next(context);
			}
			catch (NotAuthorizedException ex)
			{
				// A refusal thrown on the server: an interface-factory method's or a static
				// [Execute]'s auth check, or the operation's own (#10). It is sent as a 403
				// whose body the client rethrows as the same NotAuthorizedException; escaping,
				// it would reach the client as a 500.
				context.HttpContext.Response.Headers[NotAuthorizedResponseDto.HeaderName] = "true";
				return Results.Json(
					NotAuthorizedResponseDto.From(ex),
					DTZeroTransportJsonContext.Default.NotAuthorizedResponseDto,
					statusCode: StatusCodes.Status403Forbidden);
			}
			catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
			{
				return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
			}
			catch (OperationCanceledException) when (hostLifetime?.ApplicationStopping.IsCancellationRequested == true)
			{
				return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
			}
		});

		return app;
	}
}
