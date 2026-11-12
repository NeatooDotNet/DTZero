using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DTZero;
using DTZero.Internal;

namespace DTZeroTests.UnitTests.Internal;

/// <summary>
/// AddDTZero is safe to call more than once (#16).
/// </summary>
/// <remarks>
/// A Neatoo server reaches it twice: AddNeatooServices calls it with the domain assembly plus
/// Neatoo.dll and then swaps the JSON converter factory for its own; AddDTZeroAspNetCore calls it
/// again with the domain assembly. The second call must not replace the assembly list or bring
/// the swapped-out converter factory back. The tests stand Neatoo's half in with the same steps.
/// </remarks>
public class RepeatedRegistrationTests
{
    private static readonly System.Reflection.Assembly Domain = typeof(RepeatedRegistrationTests).Assembly;

    /// <summary>An assembly the second call does not name, standing in for Neatoo.dll.</summary>
    private static readonly System.Reflection.Assembly Library = typeof(FactAttribute).Assembly;

    private sealed class HostConverterFactory : DTZeroJsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => false;

        public override System.Text.Json.Serialization.JsonConverter? CreateConverter(Type typeToConvert, System.Text.Json.JsonSerializerOptions options) => null;
    }

    /// <summary>The steps Neatoo's AddNeatooServices takes around its own call.</summary>
    private static void AddLikeNeatoo(IServiceCollection services)
    {
        services.AddDTZero(DTZeroFactory.Server, Domain, Library);
        services.RemoveAll<DTZeroJsonConverterFactory>();
        services.AddTransient<DTZeroJsonConverterFactory, HostConverterFactory>();
    }

    [Fact]
    public void SecondCall_KeepsTheAssembliesTheFirstRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        AddLikeNeatoo(services);

        services.AddDTZero(DTZeroFactory.Server, Domain);

        using var provider = services.BuildServiceProvider();
        var assemblies = provider.GetRequiredService<IServiceAssemblies>();
        Assert.Single(services, d => d.ServiceType == typeof(IServiceAssemblies));
        Assert.Equal(typeof(FactAttribute), assemblies.FindType(typeof(FactAttribute).FullName!));
        Assert.True(assemblies.HasType(typeof(RepeatedRegistrationTests)));
    }

    [Fact]
    public void SecondCall_DoesNotBringBackAReplacedConverterFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        AddLikeNeatoo(services);

        services.AddDTZero(DTZeroFactory.Server, Domain);

        var converter = Assert.Single(services, d => d.ServiceType == typeof(DTZeroJsonConverterFactory));
        Assert.Equal(typeof(HostConverterFactory), converter.ImplementationType);
    }

    [Fact]
    public void SecondCall_WithANewAssembly_AddsItToTheExistingList()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDTZero(DTZeroFactory.Server, Domain);

        services.AddDTZero(DTZeroFactory.Server, Library);

        using var provider = services.BuildServiceProvider();
        var assemblies = provider.GetRequiredService<IServiceAssemblies>();
        Assert.Single(services, d => d.ServiceType == typeof(IServiceAssemblies));
        Assert.True(assemblies.HasType(typeof(RepeatedRegistrationTests)));
        Assert.True(assemblies.HasType(typeof(FactAttribute)));
    }

    [Fact]
    public void SecondCall_WithTheSameAssemblies_RegistersNothing()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDTZero(DTZeroFactory.Server, Domain);
        var afterFirst = services.Count;

        services.AddDTZero(DTZeroFactory.Server, Domain);

        Assert.Equal(afterFirst, services.Count);
    }

    [Fact]
    public void SecondCall_WithoutOptions_KeepsTheRegisteredOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDTZero(DTZeroFactory.Server, new DTZeroSerializationOptions { Format = SerializationFormat.Named }, Domain);

        services.AddDTZero(DTZeroFactory.Server, Domain);

        using var provider = services.BuildServiceProvider();
        Assert.Single(services, d => d.ServiceType == typeof(DTZeroSerializationOptions));
        Assert.Equal(SerializationFormat.Named, provider.GetRequiredService<DTZeroSerializationOptions>().Format);
    }

    [Fact]
    public void SecondCall_WithOptions_ReplacesTheRegisteredOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDTZero(DTZeroFactory.Server, Domain);

        services.AddDTZero(DTZeroFactory.Server, new DTZeroSerializationOptions { Format = SerializationFormat.Named }, Domain);

        using var provider = services.BuildServiceProvider();
        Assert.Single(services, d => d.ServiceType == typeof(DTZeroSerializationOptions));
        Assert.Equal(SerializationFormat.Named, provider.GetRequiredService<DTZeroSerializationOptions>().Format);
    }

    [Fact]
    public void SecondCall_WithADifferentMode_Throws()
    {
        var services = new ServiceCollection();
        services.AddDTZero(DTZeroFactory.Server, Domain);

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddDTZero(DTZeroFactory.Remote, Domain));

        Assert.Contains("DTZeroFactory.Server", ex.Message);
        Assert.Contains("DTZeroFactory.Remote", ex.Message);
    }
}
