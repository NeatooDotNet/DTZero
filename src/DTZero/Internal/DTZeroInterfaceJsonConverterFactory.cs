using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;

namespace DTZero.Internal;

public class DTZeroInterfaceJsonConverterFactory : DTZeroJsonConverterFactory
{
	private IServiceProvider scope;
	private readonly IServiceAssemblies serviceAssemblies;

	public DTZeroInterfaceJsonConverterFactory(IServiceProvider scope, IServiceAssemblies serviceAssemblies)
	{
		this.scope = scope;
		this.serviceAssemblies = serviceAssemblies;
	}

	public override bool CanConvert(Type typeToConvert)
	{
		ArgumentNullException.ThrowIfNull(typeToConvert, nameof(typeToConvert));

		if ((typeToConvert.IsInterface || typeToConvert.IsAbstract) && !typeToConvert.IsGenericType && this.serviceAssemblies.HasType(typeToConvert))
		{
			return true;
		}

		return false;
	}

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(typeToConvert, nameof(typeToConvert));

		if (typeToConvert.IsInterface || typeToConvert.IsAbstract)
		{
			return (JsonConverter)this.scope.GetRequiredService(typeof(DTZeroInterfaceJsonTypeConverter<>).MakeGenericType(typeToConvert));
		}

		throw new JsonException($"CreateConverter not implemented for {typeToConvert}");
	}
}
