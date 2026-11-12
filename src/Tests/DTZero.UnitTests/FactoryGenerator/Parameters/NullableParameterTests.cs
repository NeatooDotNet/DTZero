using Microsoft.Extensions.DependencyInjection;
using DTZeroTests.UnitTests.Shared;
using DTZeroTests.UnitTests.TestContainers;
using DTZeroTests.UnitTests.TestTargets.Parameters;

namespace DTZeroTests.UnitTests.FactoryGenerator.Parameters;

/// <summary>
/// Unit tests for factory methods with nullable parameters.
/// </summary>
public class NullableParameterTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public NullableParameterTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    [Fact]
    public void Create_WithNullValue_Works()
    {
        var factory = _provider.GetRequiredService<INullableParameterTargetFactory>();

        var result = factory.Create(null);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    [Fact]
    public void Create_WithValue_Works()
    {
        var factory = _provider.GetRequiredService<INullableParameterTargetFactory>();

        var result = factory.Create(42);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }
}
