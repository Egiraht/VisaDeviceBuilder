// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Threading;
using System.Threading.Tasks;
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
    private const int TestAutoUpdaterDelay = 1;

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
      await using var device = new MessageDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName
      };
      await using var controller = new VisaDeviceController(device)
      {
        AutoUpdaterDelay = TestAutoUpdaterDelay,
        IsAutoUpdaterEnabled = false
      };

      // Checking the controller's properties.
      Assert.Same(device, controller.Device);
      Assert.Equal(TestAutoUpdaterDelay, controller.AutoUpdaterDelay);
      Assert.NotNull(controller.AutoUpdater);
      Assert.False(controller.IsAutoUpdaterEnabled);
      Assert.True(controller.CanConnect);
      Assert.Empty(controller.Identifier);
      Assert.False(controller.IsDeviceReady);
    }

    /// <summary>
    ///   Testing VISA device connection using a controller.
    /// </summary>
    [Fact]
    public async Task DeviceConnectionTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new TestMessageDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName
      };
      await using var controller = new VisaDeviceController(device) { IsAutoUpdaterEnabled = false };
      var connected = false;
      var disconnected = false;
      controller.Connected += (_, _) => connected = true;
      controller.Disconnected += (_, _) => disconnected = true;
      Assert.True(controller.CanConnect);
      Assert.False(controller.IsDeviceReady);

      // Connecting to the device.
      controller.BeginConnect();
      controller.BeginConnect(); // Repeated call should pass OK.
      Assert.False(controller.CanConnect);
      Assert.False(controller.IsDeviceReady);
      Assert.False(connected);

      await controller.GetDeviceConnectionTask();
      Assert.False(controller.CanConnect);
      Assert.True(controller.IsDeviceReady);
      Assert.Equal(TestResourceManager.SerialTestDeviceAliasName, controller.Identifier);
      Assert.True(connected);

      // Disconnecting from the device.
      controller.BeginDisconnect();
      controller.BeginDisconnect(); // Repeated call should pass OK.
      Assert.False(controller.CanConnect);
      Assert.False(controller.IsDeviceReady);
      Assert.Empty(controller.Identifier);
      Assert.False(disconnected);

      await controller.GetDeviceDisconnectionTask();
      Assert.True(controller.CanConnect);
      Assert.False(controller.IsDeviceReady);
      Assert.True(disconnected);
    }

    /// <summary>
    ///   Testing VISA device connection interruption using a controller.
    /// </summary>
    [Fact]
    public async Task DeviceConnectionInterruptionTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new TestMessageDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName
      };
      await using var controller = new VisaDeviceController(device);
      var connected = false;
      var disconnected = false;
      controller.Connected += (_, _) => connected = true;
      controller.Disconnected += (_, _) => disconnected = true;

      // Interrupting the connection process immediately.
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
      await using var device = new TestMessageDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName
      };
      await using var controller = new VisaDeviceController(device) { IsAutoUpdaterEnabled = false };
      controller.Exception += (_, args) => throw args.Exception;

      // Throw an exceptions if updating when the device is disconnected.
      await Assert.ThrowsAsync<VisaDeviceException>(controller.UpdateAsyncPropertiesAsync);

      // The device is connected.
      controller.BeginConnect();
      await controller.GetDeviceConnectionTask();
      var updateAsyncPropertiesTask = controller.UpdateAsyncPropertiesAsync();
      _ = controller.UpdateAsyncPropertiesAsync(); // Repeated simultaneous call should pass OK.
      await updateAsyncPropertiesTask;
    }

    /// <summary>
    ///   Testing control of the auto-updater attached to a controller.
    /// </summary>
    [Fact]
    public async Task AutoUpdaterControlTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new TestMessageDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName
      };
      await using var controller = new VisaDeviceController(device)
      {
        IsAutoUpdaterEnabled = false,
        AutoUpdaterDelay = default
      };
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
      await using var device = new TestMessageDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName
      };
      await using var controller = new VisaDeviceController(device)
      {
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
      Assert.Equal(DeviceConnectionState.DisconnectedWithError, device.ConnectionState);

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
      Assert.Equal(DeviceConnectionState.DisconnectedWithError, device.ConnectionState);

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
      Assert.Equal(DeviceConnectionState.Connected, device.ConnectionState);

      // Testing normal disconnection after the device is ready.
      controller.BeginDisconnect();
      await controller.GetDeviceDisconnectionTask();
      Assert.Equal(DeviceConnectionState.Disconnected, device.ConnectionState);
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
      await Assert.ThrowsAsync<ObjectDisposedException>(controller.UpdateAsyncPropertiesAsync);

      // Repeated device disposals should pass OK.
      controller.Dispose();
      await controller.DisposeAsync();
    }
  }
}
