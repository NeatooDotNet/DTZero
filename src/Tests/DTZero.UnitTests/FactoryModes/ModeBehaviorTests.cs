using Microsoft.Extensions.DependencyInjection;
using DTZero;
using DTZero.Internal;
using DTZeroTests.UnitTests.Logical;
using DTZeroTests.UnitTests.Shared;
using DTZeroTests.UnitTests.TestContainers;
using System.Reflection;

namespace DTZeroTests.UnitTests.FactoryModes;

/// <summary>
/// Tests that verify correct constructor selection and behavior for each DTZeroFactory mode.
/// These tests ensure that Logical mode behaves like Server mode (no serialization).
/// </summary>
public class ModeBehaviorTests
{
    #region DI Registration Tests

    [Fact]
    public void ServerMode_DoesNotRegister_IMakeRemoteDelegateRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDTZero(
            DTZeroFactory.Server,
            new DTZeroSerializationOptions(),
            Assembly.GetExecutingAssembly());

        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetService<IMakeRemoteDelegateRequest>();

        // Assert - Server mode should NOT register IMakeRemoteDelegateRequest
        Assert.Null(service);
    }

    [Fact]
    public void LogicalMode_DoesNotRegister_IMakeRemoteDelegateRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDTZero(
            DTZeroFactory.Logical,
            new DTZeroSerializationOptions(),
            Assembly.GetExecutingAssembly());

        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetService<IMakeRemoteDelegateRequest>();

        // Assert - Logical mode should NOT register IMakeRemoteDelegateRequest (fixed behavior)
        Assert.Null(service);
    }

    [Fact]
    public void RemoteMode_Registers_IMakeRemoteDelegateRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDTZero(
            DTZeroFactory.Remote,
            new DTZeroSerializationOptions(),
            Assembly.GetExecutingAssembly());
        // Remote mode requires an HttpClient
        services.AddKeyedScoped<HttpClient>(DTZeroServices.HttpClientKey,
            (sp, key) => new HttpClient());

        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetService<IMakeRemoteDelegateRequest>();

        // Assert - Remote mode SHOULD register IMakeRemoteDelegateRequest
        Assert.NotNull(service);
        Assert.IsType<MakeRemoteDelegateRequest>(service);
    }

    #endregion

    #region Constructor Selection Tests

    [Fact]
    public void ServerMode_UsesLocalConstructor()
    {
        // Arrange
        var provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ILogicalModeTarget_RemoteModeFactory>();

        // Act - Get the underlying factory and check MakeRemoteDelegateRequest field
        var factoryType = factory.GetType();
        var delegateField = factoryType.GetField("MakeRemoteDelegateRequest",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Assert - Local constructor leaves MakeRemoteDelegateRequest null
        Assert.NotNull(delegateField);
        var delegateValue = delegateField.GetValue(factory);
        Assert.Null(delegateValue);
    }

    [Fact]
    public void LogicalMode_UsesLocalConstructor()
    {
        // Arrange
        var provider = new LogicalContainerBuilder()
            .WithService<IService, Service>()
            .Build();

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ILogicalModeTarget_RemoteModeFactory>();

        // Act - Get the underlying factory and check MakeRemoteDelegateRequest field
        var factoryType = factory.GetType();
        var delegateField = factoryType.GetField("MakeRemoteDelegateRequest",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Assert - Local constructor leaves MakeRemoteDelegateRequest null (same as Server mode)
        Assert.NotNull(delegateField);
        var delegateValue = delegateField.GetValue(factory);
        Assert.Null(delegateValue);
    }

    #endregion

    #region Behavioral Equivalence Tests

    [Fact]
    public async Task ServerMode_Insert_ExecutesLocally()
    {
        // Arrange
        var provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ILogicalModeTarget_RemoteModeFactory>();
        var entity = new LogicalModeTarget_RemoteMode { IsNew = true };

        // Act
        var result = await factory.Save(entity);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    [Fact]
    public async Task LogicalMode_Insert_ExecutesLocally()
    {
        // Arrange
        var provider = new LogicalContainerBuilder()
            .WithService<IService, Service>()
            .Build();

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ILogicalModeTarget_RemoteModeFactory>();
        var entity = new LogicalModeTarget_RemoteMode { IsNew = true };

        // Act
        var result = await factory.Save(entity);

        // Assert - Should behave identically to Server mode
        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    [Fact]
    public async Task ServerAndLogicalModes_ProduceIdenticalResults()
    {
        // Arrange - Server mode
        var serverProvider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();
        using var serverScope = serverProvider.CreateScope();
        var serverFactory = serverScope.ServiceProvider.GetRequiredService<ILogicalModeTarget_RemoteModeFactory>();
        var serverEntity = new LogicalModeTarget_RemoteMode { IsNew = true };

        // Arrange - Logical mode
        var logicalProvider = new LogicalContainerBuilder()
            .WithService<IService, Service>()
            .Build();
        using var logicalScope = logicalProvider.CreateScope();
        var logicalFactory = logicalScope.ServiceProvider.GetRequiredService<ILogicalModeTarget_RemoteModeFactory>();
        var logicalEntity = new LogicalModeTarget_RemoteMode { IsNew = true };

        // Act
        var serverResult = await serverFactory.Save(serverEntity);
        var logicalResult = await logicalFactory.Save(logicalEntity);

        // Assert - Both modes should produce identical results
        Assert.NotNull(serverResult);
        Assert.NotNull(logicalResult);
        Assert.Equal(serverResult.InsertCalled, logicalResult.InsertCalled);
        Assert.Equal(serverResult.UpdateCalled, logicalResult.UpdateCalled);
        Assert.Equal(serverResult.DeleteCalled, logicalResult.DeleteCalled);
    }

    #endregion
}
