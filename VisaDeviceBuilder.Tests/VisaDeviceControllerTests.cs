using System;
using System.Threading;
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
    ///   Defines the test auto-updater delay value in milliseconds.
    /// </summary>
    public const int TestAutoUpdaterDelay = 12;

    /// <summary>
    ///   Defines the asynchronous operation timeout period.
    /// </summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

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
      Assert.Contains(controller.DeviceActions, deviceAction => deviceAction.DeviceActionDelegate == device.Reset);
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
      var connected = false;
      var disconnected = false;
      controller.Connected += (_, _) => connected = true;
      controller.Disconnected += (_, _) => disconnected = true;
      Assert.True(controller.CanConnect);
      Assert.False(controller.IsDeviceReady);
      Assert.False(controller.IsDisconnectionRequested);

      // Connecting to the device.
      controller.BeginConnect();
      controller.BeginConnect(); // Repeated call should pass OK.
      Assert.False(controller.CanConnect);
      Assert.False(controller.IsDeviceReady);
      Assert.False(controller.IsDisconnectionRequested);
      Assert.False(connected);

      await controller.GetDeviceConnectionTask();
      Assert.False(controller.CanConnect);
      Assert.True(controller.IsDeviceReady);
      Assert.False(controller.IsDisconnectionRequested);
      Assert.Equal(TestResourceManager.SerialTestDeviceAliasName, controller.Identifier);
      Assert.True(connected);

      // Disconnecting from the device.
      controller.BeginDisconnect();
      controller.BeginDisconnect(); // Repeated call should pass OK.
      Assert.False(controller.CanConnect);
      Assert.False(controller.IsDeviceReady);
      Assert.True(controller.IsDisconnectionRequested);
      Assert.Empty(controller.Identifier);
      Assert.False(disconnected);

      await controller.GetDeviceDisconnectionTask();
      Assert.True(controller.CanConnect);
      Assert.False(controller.IsDeviceReady);
      Assert.False(controller.IsDisconnectionRequested);
      Assert.True(disconnected);
    }

    /// <summary>
    ///   Testing VISA device connection interruption using a controller.
    /// </summary>
    [Fact]
    public async Task DeviceConnectionInterruptionTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new TestMessageDevice();
      await using var controller = new VisaDeviceController(device)
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName
      };
      var connected = false;
      var disconnected = false;
      controller.Connected += (_, _) => connected = true;
      controller.Disconnected += (_, _) => disconnected = true;

      // Interrupting the connection process before it finishes.
      controller.BeginConnect();
      controller.BeginDisconnect();
      await controller.GetDeviceDisconnectionTask();
      Assert.False(connected);
      Assert.True(disconnected);
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
      controller.BeginConnect();
      await controller.GetDeviceConnectionTask();
      var updateAsyncPropertiesTask = controller.UpdateAsyncPropertiesAsync();
      Assert.True(controller.IsUpdatingAsyncProperties);
      _ = controller.UpdateAsyncPropertiesAsync(); // Repeated simultaneous call should pass OK.
      await updateAsyncPropertiesTask;
      Assert.False(controller.IsUpdatingAsyncProperties);
    }

    /// <summary>
    ///   Testing control of the auto-updater attached to a controller.
    /// </summary>
    [Fact]
    public async Task AutoUpdaterControlTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new TestMessageDevice();
      await using var controller = new VisaDeviceController(device)
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName,
        IsAutoUpdaterEnabled = false,
        AutoUpdaterDelay = default
      };
      Assert.Equal(controller.AsyncProperties, controller.AutoUpdater.AsyncProperties);
      Assert.False(controller.AutoUpdater.IsRunning);
      Assert.Equal(default, controller.AutoUpdater.Delay);

      // Auto-updater must remain disabled after device connection.
      controller.BeginConnect();
      await controller.GetDeviceConnectionTask();
      Assert.False(controller.AutoUpdater.IsRunning);

      // Changing the auto-updater state when the device is connected must be OK.
      controller.IsAutoUpdaterEnabled = true;
      Assert.True(controller.AutoUpdater.IsRunning);
      controller.IsAutoUpdaterEnabled = false;
      var timer = Task.Delay(Timeout);
      while (controller.AutoUpdater.IsRunning && !timer.IsCompleted) // The operation should complete before the timer.
        await Task.Delay(controller.AutoUpdater.Delay);
      Assert.False(controller.AutoUpdater.IsRunning);

      // Changing the auto-updater delay value when it is running must be OK.
      controller.IsAutoUpdaterEnabled = true;
      controller.AutoUpdaterDelay = TestAutoUpdaterDelay;
      Assert.Equal(TestAutoUpdaterDelay, controller.AutoUpdater.Delay.TotalMilliseconds);

      // Auto-updater must get disabled after device disconnection.
      controller.BeginDisconnect();
      await controller.GetDeviceDisconnectionTask();
      Assert.True(controller.IsAutoUpdaterEnabled);
      Assert.False(controller.AutoUpdater.IsRunning);
    }

    /// <summary>
    ///   Testing the device exceptions handling by the controller.
    /// </summary>
    [Fact]
    public async Task DeviceExceptionsTest()
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
      static void ExceptionCallback(object _, ThreadExceptionEventArgs args) => throw args.Exception;
      controller.Exception += ExceptionCallback;

      // Testing possible exceptions thrown during the device initialization process.
      // The device must be automatically disconnected at this stage.
      device.ThrowOnInitialization = true;
      device.ThrowOnAsyncPropertyGetter = false;
      device.ThrowOnAsyncPropertySetter = false;
      controller.BeginConnect();
      await Assert.ThrowsAsync<VisaDeviceException>(controller.GetDeviceConnectionTask);
      Assert.False(controller.IsDeviceReady);
      await controller.GetDeviceDisconnectionTask();
      Assert.True(controller.CanConnect);

      // Testing possible exceptions thrown when getting the initial values of the device's asynchronous properties.
      // The device must be automatically disconnected at this stage.
      device.ThrowOnInitialization = false;
      device.ThrowOnAsyncPropertyGetter = true;
      device.ThrowOnAsyncPropertySetter = false;
      controller.BeginConnect();
      await Assert.ThrowsAsync<VisaDeviceException>(controller.GetDeviceConnectionTask);
      Assert.False(controller.IsDeviceReady);
      await controller.GetDeviceDisconnectionTask();
      Assert.True(controller.CanConnect);

      // Testing possible exceptions during auto-updater cycles after the device is ready.
      // The device must remain connected at this stage.
      device.ThrowOnInitialization = false;
      device.ThrowOnAsyncPropertyGetter = false;
      device.ThrowOnAsyncPropertySetter = true;
      controller.Exception -= ExceptionCallback;
      controller.BeginConnect();
      await controller.GetDeviceConnectionTask();
      Assert.True(controller.IsDeviceReady);
      await controller.GetDeviceDisconnectionTask(); // Should do nothing when no disconnection is intended.
      Assert.False(controller.CanConnect);
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
        controller.BeginConnect();

      // The controller instance now should be disposed of.
      Assert.False(controller.IsDeviceReady);
      Assert.Throws<ObjectDisposedException>(controller.BeginConnect);
      Assert.Throws<ObjectDisposedException>(controller.BeginDisconnect);
      await Assert.ThrowsAsync<ObjectDisposedException>(controller.UpdateResourcesListAsync);
      await Assert.ThrowsAsync<ObjectDisposedException>(controller.UpdateAsyncPropertiesAsync);

      // Repeated device disposals should pass OK.
      controller.Dispose();
      await controller.DisposeAsync();
    }
  }
}
