using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using DTZero.Internal;

namespace DTZero.AspNetCore;

public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds DTZero ASP.NET Core services with default serialization options (Ordinal format).
	/// </summary>
	/// <remarks>
	/// May be called before or after Neatoo's AddNeatooServices(DTZeroFactory.Server, ...):
	/// the second registration adds only what is new (#16). Options an earlier call
	/// registered are kept.
	/// </remarks>
	public static IServiceCollection AddDTZeroAspNetCore(this IServiceCollection services, params Assembly[] entityLibraries)
	{
		services.AddDTZero(DTZeroFactory.Server, entityLibraries);
		return AddAspNetCoreServices(services);
	}

	/// <summary>
	/// Adds DTZero ASP.NET Core services with custom serialization options.
	/// </summary>
	/// <remarks>
	/// May be called before or after Neatoo's AddNeatooServices(DTZeroFactory.Server, ...).
	/// The options passed here replace any registered earlier.
	/// </remarks>
	public static IServiceCollection AddDTZeroAspNetCore(this IServiceCollection services, DTZeroSerializationOptions serializationOptions, params Assembly[] entityLibraries)
	{
		services.AddDTZero(DTZeroFactory.Server, serializationOptions, entityLibraries);
		return AddAspNetCoreServices(services);
	}

	private static IServiceCollection AddAspNetCoreServices(IServiceCollection services)
	{
		services.TryAddScoped<IAspAuthorize, AspAuthorize>();

		// Register the HandleRemoteDelegateRequest with logger injection
		services.AddScoped<HandleRemoteDelegateRequest>(sp =>
		{
			var loggerFactory = sp.GetService<ILoggerFactory>();
			var logger = loggerFactory?.CreateLogger(DTZeroLoggerCategories.Server);
			return LocalServer.HandlePortalRequest(sp, logger);
		});

		return services;
	}
}
