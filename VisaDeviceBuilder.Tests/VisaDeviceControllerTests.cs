using System;
using System.Threading.Tasks;
using Moq;
using VisaDeviceBuilder.Abstracts;
using VisaDeviceBuilder.Tests.Components;
using Xunit;
using LocalizationResourceManager = System.Resources.ResourceManager;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="VisaDeviceController" /> class.
  /// </summary>
  public class VisaDeviceControllerTests
  {
    /// <summary>
    ///   Defines the delay after that the test session state check is performed.
    /// </summary>
    public const int StateCheckPeriod = 30;

    /// <summary>
    ///   Defines the test auto-updater delay value in milliseconds.
    /// </summary>
    public const int TestAutoUpdaterDelay = 12;

    /// <summary>
    ///   Testing new VISA device controller instance creation.
    /// </summary>
    [Fact]
    public async Task VisaDeviceControllerTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new MessageDevice();
      var localizationResourceManager = new Mock<LocalizationResourceManager>().Object;
      await using var controller = new VisaDeviceController(device)
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName,
        AutoUpdaterDelay = TestAutoUpdaterDelay,
        IsAutoUpdaterEnabled = false,
        LocalizationResourceManager = localizationResourceManager
      };

      // Checking the controller's properties.
      Assert.Same(device, controller.Device);
      Assert.True(controller.IsMessageDevice);
      Assert.Same(resourceManager, controller.ResourceManager);
      Assert.Equal(TestResourceManager.SerialTestDeviceResourceName, controller.ResourceName);
      Assert.Equal(TestAutoUpdaterDelay, controller.AutoUpdaterDelay);
      Assert.NotNull(controller.AutoUpdater);
      Assert.False(controller.IsAutoUpdaterEnabled);
      Assert.Same(localizationResourceManager, controller.LocalizationResourceManager);
      Assert.True(controller.CanConnect);
      Assert.Empty(controller.Identifier);
      Assert.False(controller.IsDeviceReady);
      Assert.Empty(controller.AvailableVisaResources);
      Assert.False(controller.IsUpdatingVisaResources);
      Assert.False(controller.IsUpdatingAsyncProperties);
      Assert.Empty(controller.AsyncProperties);
      Assert.Contains(controller.DeviceActions, deviceAction => (DeviceAction) deviceAction == (Action) device.Reset);
      Assert.False(controller.IsDisconnectionRequested);
    }

    /// <summary>
    ///   Testing discovery of available VISA resources using a controller.
    /// </summary>
    [Fact]
    public async Task VisaResourcesListTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new TestMessageDevice();
      await using var controller = new VisaDeviceController(device) {ResourceManager = resourceManager};
      Assert.Empty(controller.AvailableVisaResources); // Available resources should be empty after controller creation.
      Assert.False(controller.IsUpdatingVisaResources);

      // Updating available VISA resources.
      var updateResourcesListTask = controller.UpdateResourcesListAsync();
      Assert.True(controller.IsUpdatingVisaResources);
      _ = controller.UpdateResourcesListAsync(); // Repeated simultaneous call should pass.
      await updateResourcesListTask;
      Assert.False(controller.IsUpdatingVisaResources);

      // Resources from the TestResourceManager must be included into the controller's available resources list.
      var resources = await VisaResourceLocator.LocateResourceNamesAsync<TestResourceManager>();
      Assert.Equal(resources, controller.AvailableVisaResources);
    }

    /// <summary>
    ///   Testing VISA device connection using a controller.
    /// </summary>
    [Fact]
    public async Task DeviceConnectionTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new TestMessageDevice();
      await using var controller = new VisaDeviceController(device)
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName,
        IsAutoUpdaterEnabled = false
      };
      Assert.True(controller.CanConnect);
      Assert.False(controller.IsDeviceReady);
      Assert.False(controller.IsDisconnectionRequested);

      controller.Connect();
      controller.Connect(); // Repeated call should pass OK.
      Assert.False(controller.CanConnect);
      Assert.False(controller.IsDeviceReady);
      Assert.False(controller.IsDisconnectionRequested);

      // Connecting to the device.
      do
        await Task.Delay(StateCheckPeriod);
      while (!controller.IsDeviceReady);
      Assert.False(controller.CanConnect);
      Assert.True(controller.IsDeviceReady);
      Assert.False(controller.IsDisconnectionRequested);
      Assert.Equal(TestResourceManager.SerialTestDeviceAliasName, controller.Identifier);

      // Disconnecting from the device.
      var disconnectionTask = controller.DisconnectAsync();
      _ = controller.DisconnectAsync(); // Repeated simultaneous call should pass OK.
      Assert.False(controller.CanConnect);
      Assert.False(controller.IsDeviceReady);
      Assert.True(controller.IsDisconnectionRequested);

      await disconnectionTask;
      Assert.True(controller.CanConnect);
      Assert.False(controller.IsDeviceReady);
      Assert.False(controller.IsDisconnectionRequested);
    }

    /// <summary>
    ///   Testing manual updating of asynchronous properties using a controller.
    /// </summary>
    [Fact]
    public async Task ManualAsyncPropertiesUpdateTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new TestMessageDevice();
      await using var controller = new VisaDeviceController(device)
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName,
        IsAutoUpdaterEnabled = false
      };
      controller.Exception += (_, args) => throw args.Exception;

      // Throw an exceptions if updating when the device is disconnected.
      await Assert.ThrowsAsync<VisaDeviceException>(controller.UpdateAsyncPropertiesAsync);

      // The device is connected.
      controller.Connect();
      do
        await Task.Delay(StateCheckPeriod);
      while (!controller.IsDeviceReady);
      var updateAsyncPropertiesTask = controller.UpdateAsyncPropertiesAsync();
      Assert.True(controller.IsUpdatingAsyncProperties);
      _ = controller.UpdateAsyncPropertiesAsync(); // Repeated simultaneous call should pass OK.
      await updateAsyncPropertiesTask;
      Assert.False(controller.IsUpdatingAsyncProperties);
    }

    /// <summary>
    ///   Testing the controller exceptions handling during the device initialization.
    /// </summary>
    [Fact]
    public async Task DeviceInitializationExceptionTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new TestMessageDevice();
      await using var controller = new VisaDeviceController(device)
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName,
        IsAutoUpdaterEnabled = true,
        AutoUpdaterDelay = TestAutoUpdaterDelay
      };

      var isExceptionCaught = false;
      object? eventSender = null;
      Exception? exception = null;
      controller.Exception += (sender, args) =>
      {
        isExceptionCaught = true;
        eventSender = sender;
        exception = args.Exception;
      };

      // The exception will be thrown during the device initial asynchronous properties update.
      // The device should occur in the "disconnected with error" state because the exception is caught during
      // the device initialization stage.
      controller.Connect();
      ((TestMessageDevice) controller.Device!).ThrowOnInitialization = true;
      do
        await Task.Delay(StateCheckPeriod);
      while (!isExceptionCaught);
      Assert.Equal(controller, eventSender);
      Assert.NotNull(exception);
    }

    /// <summary>
    ///   Testing the controller exceptions handling during the device auto-updating.
    /// </summary>
    [Fact]
    public async Task DeviceAutoUpdatingExceptionTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new TestMessageDevice();
      await using var controller = new VisaDeviceController(device)
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName,
        IsAutoUpdaterEnabled = true,
        AutoUpdaterDelay = TestAutoUpdaterDelay
      };

      var isExceptionCaught = false;
      object? eventSender = null;
      Exception? exception = null;
      controller.Exception += (sender, args) =>
      {
        isExceptionCaught = true;
        eventSender = sender;
        exception = args.Exception;
      };

      // The exception will be thrown after the device initialization and will be caught by the auto-updater.
      // The device should remain in the "connected" state after exception is caught.
      controller.Connect();
      do
        await Task.Delay(StateCheckPeriod);
      while (!controller.IsDeviceReady);
      Assert.False(isExceptionCaught);

      ((TestMessageDevice) controller.Device!).ThrowOnAsyncPropertyGetter = true;
      do
        await Task.Delay(StateCheckPeriod);
      while (!isExceptionCaught);
      Assert.Equal(controller, eventSender);
      Assert.NotNull(exception);
    }

    /// <summary>
    ///   Testing VISA device controller object disposal.
    /// </summary>
    [Fact]
    public async Task VisaDeviceControllerDisposalTest()
    {
      IVisaDeviceController? controller;
      await using var device = new TestMessageDevice();
      await using (controller = new VisaDeviceController(device))
        controller.Connect();

      // The controller instance now should be disposed of.
      Assert.False(controller.IsDeviceReady);
      Assert.Throws<ObjectDisposedException>(controller.Connect);
      await Assert.ThrowsAsync<ObjectDisposedException>(controller.UpdateResourcesListAsync);
      await Assert.ThrowsAsync<ObjectDisposedException>(controller.UpdateAsyncPropertiesAsync);
      await Assert.ThrowsAsync<ObjectDisposedException>(controller.DisconnectAsync);

      // Repeated device disposals should pass OK.
      controller.Dispose();
      await controller.DisposeAsync();
    }

    // TODO: Add tests for auto-updater control and names localization.
  }
}
