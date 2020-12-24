using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using VisaDeviceBuilder.Tests.Components;
using Xunit;

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
    public const int StateCheckPeriod = 10;

    /// <summary>
    ///   Defines the test auto-updater delay value in milliseconds.
    /// </summary>
    public const int TestAutoUpdaterDelay = 5;

    /// <summary>
    ///   Testing the available VISA resources discovery logic.
    /// </summary>
    [Fact]
    public async Task VisaResourcesListTest()
    {
      await using var controller = new VisaDeviceController {VisaResourceManagerType = typeof(TestResourceManager)};
      Assert.Empty(controller.AvailableVisaResources);
      Assert.False(controller.IsUpdatingVisaResources);

      var updateResourcesListTask = controller.UpdateResourcesListAsync();
      _ = controller.UpdateResourcesListAsync(); // Repeated simultaneous call.
      Assert.True(controller.IsUpdatingVisaResources);

      // Checking the presence of the test resource names and their aliases in the updated VISA resources list.
      await updateResourcesListTask;
      using var resourceManager = new TestResourceManager();
      var testResourceNames = resourceManager.Find().Reverse().Aggregate(new List<string>(), (results, current) =>
      {
        var parseResult = resourceManager.Parse(current);
        results.Add(parseResult.OriginalResourceName);
        if (!string.IsNullOrEmpty(parseResult.AliasIfExists))
          results.Add(parseResult.AliasIfExists);
        return results;
      });
      Assert.Equal(testResourceNames.ToImmutableSortedSet(), controller.AvailableVisaResources.ToImmutableSortedSet());
    }

    /// <summary>
    ///   Testing wrong VISA device and resource manager types detection.
    /// </summary>
    [Fact]
    public void WrongDeviceTypeTest()
    {
      Assert.ThrowsAny<Exception>(() =>
      {
        using var controller = new VisaDeviceController
        {
          DeviceType = typeof(object),
          VisaResourceManagerType = typeof(TestResourceManager),
          ResourceName = TestResourceManager.CustomTestDeviceResourceName
        };
      });
      Assert.ThrowsAny<Exception>(() =>
      {
        using var controller = new VisaDeviceController
        {
          DeviceType = typeof(TestMessageDevice),
          VisaResourceManagerType = typeof(object),
          ResourceName = TestResourceManager.CustomTestDeviceResourceName
        };
      });
    }

    /// <summary>
    ///   Testing the VISA message device controller logic.
    /// </summary>
    [Fact]
    public async Task MessageDeviceControllerTest()
    {
      await using var controller = new VisaDeviceController
      {
        DeviceType = typeof(TestMessageDevice),
        VisaResourceManagerType = typeof(TestResourceManager),
        ResourceName = TestResourceManager.SerialTestDeviceResourceName,
        AutoUpdaterDelay = TestAutoUpdaterDelay,
        IsAutoUpdaterEnabled = true,
        LocalizationResourceManager = null
      };
      Assert.Equal(typeof(TestMessageDevice), controller.DeviceType);
      Assert.Equal(typeof(TestResourceManager), controller.VisaResourceManagerType);
      Assert.Equal(typeof(TestResourceManager), controller.VisaResourceManagerType);
      Assert.Equal(TestResourceManager.SerialTestDeviceResourceName, controller.ResourceName);
      Assert.Equal(TestAutoUpdaterDelay, controller.AutoUpdaterDelay);
      Assert.True(controller.IsAutoUpdaterEnabled);
      Assert.Null(controller.LocalizationResourceManager);
      Assert.True(controller.IsMessageDevice);
      Assert.True(controller.CanConnect);
      Assert.Equal(DeviceConnectionState.Disconnected, controller.ConnectionState);
      Assert.Empty(controller.Identifier);
      Assert.False(controller.IsDeviceReady);
      Assert.False(controller.IsUpdatingAsyncProperties);
      Assert.False(controller.IsDisconnectionRequested);

      controller.Connect();
      controller.Connect(); // Repeated call.
      Assert.False(controller.CanConnect);

      // Testing the connection session that remains opened until disconnection is requested.
      do
        await Task.Delay(StateCheckPeriod);
      while (!controller.IsDeviceReady);
      Assert.Equal(DeviceConnectionState.Connected, controller.ConnectionState);
      Assert.Equal(TestResourceManager.SerialTestDeviceAliasName, controller.Identifier);
      Assert.NotEmpty(controller.AsyncProperties);
      Assert.Equal(nameof(TestMessageDevice.TestAsyncProperty), controller.AsyncProperties[0].Name);
      Assert.NotEmpty(controller.DeviceActions);
      Assert.Equal(nameof(TestMessageDevice.TestDeviceAction), controller.DeviceActions[0].Name);
      Assert.True(controller.IsDeviceReady);
      Assert.False(controller.IsDisconnectionRequested);

      // Testing the disconnection request.
      var disconnectionTask = controller.DisconnectAsync();
      _ = controller.DisconnectAsync(); // Repeated simultaneous call.
      Assert.True(controller.IsDisconnectionRequested);
      await disconnectionTask;
      Assert.Empty(controller.Identifier);
      Assert.Equal(DeviceConnectionState.Disconnected, controller.ConnectionState);
      Assert.True(controller.CanConnect);
      Assert.False(controller.IsDeviceReady);
      Assert.False(controller.IsUpdatingAsyncProperties);
      Assert.False(controller.IsDisconnectionRequested);

      // Testing the connection session that will be closed immediately on the controller disposal.
      controller.Connect();
    }

    /// <summary>
    ///   Testing the manual asynchronous properties update logic.
    /// </summary>
    [Fact]
    public async Task ManualAsyncPropertiesUpdateTest()
    {
      await using var controller = new VisaDeviceController
      {
        DeviceType = typeof(TestMessageDevice),
        VisaResourceManagerType = typeof(TestResourceManager),
        ResourceName = TestResourceManager.SerialTestDeviceResourceName,
        IsAutoUpdaterEnabled = false
      };

      // The device is disconnected.
      var updateAsyncPropertiesTask = controller.UpdateAsyncPropertiesAsync();
      Assert.False(controller.IsUpdatingAsyncProperties);
      await updateAsyncPropertiesTask;
      Assert.False(controller.IsUpdatingAsyncProperties);

      // The device is connected.
      controller.Connect();
      do
        await Task.Delay(StateCheckPeriod);
      while (!controller.IsDeviceReady);
      updateAsyncPropertiesTask = controller.UpdateAsyncPropertiesAsync();
      _ = controller.UpdateAsyncPropertiesAsync(); // Repeated simultaneous call.
      Assert.True(controller.IsUpdatingAsyncProperties);
      await updateAsyncPropertiesTask;
      Assert.False(controller.IsUpdatingAsyncProperties);
    }

    /// <summary>
    ///   Testing the controller exceptions handling during the device initialization.
    /// </summary>
    [Fact]
    public async Task DeviceInitializationExceptionTest()
    {
      var isExceptionCaught = false;
      object? eventSender = null;
      Exception? exception = null;
      await using var controller = new VisaDeviceController
      {
        DeviceType = typeof(TestMessageDevice),
        VisaResourceManagerType = typeof(TestResourceManager),
        ResourceName = TestResourceManager.SerialTestDeviceResourceName,
        IsAutoUpdaterEnabled = true,
        AutoUpdaterDelay = TestAutoUpdaterDelay
      };
      controller.BeforeInitialization += (_, device) =>
        ((TestMessageDevice) device).ThrowOnAsyncPropertyGetter = true;
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
      do
        await Task.Delay(StateCheckPeriod);
      while (!isExceptionCaught);
      Assert.Equal(controller, eventSender);
      Assert.Equal(TestMessageDevice.TestExceptionText, exception?.Message);
      Assert.Equal(DeviceConnectionState.DisconnectedWithError, controller.ConnectionState);
    }

    /// <summary>
    ///   Testing the controller exceptions handling during the device auto-updating.
    /// </summary>
    [Fact]
    public async Task DeviceAutoUpdatingExceptionTest()
    {
      var isExceptionCaught = false;
      object? eventSender = null;
      Exception? exception = null;
      await using var controller = new VisaDeviceController
      {
        DeviceType = typeof(TestMessageDevice),
        VisaResourceManagerType = typeof(TestResourceManager),
        ResourceName = TestResourceManager.SerialTestDeviceResourceName,
        IsAutoUpdaterEnabled = true,
        AutoUpdaterDelay = TestAutoUpdaterDelay
      };
      controller.AfterInitialization += (_, device) =>
        ((TestMessageDevice) device).ThrowOnAsyncPropertyGetter = true;
      controller.Exception += (sender, args) =>
      {
        isExceptionCaught = true;
        eventSender = sender;
        exception = args.Exception;
      };

      // The device initialization stage should pass OK.
      isExceptionCaught = false;
      eventSender = null;
      exception = null;
      controller.Connect();
      do
        await Task.Delay(StateCheckPeriod);
      while (!controller.IsDeviceReady);
      Assert.False(isExceptionCaught);
      Assert.Null(eventSender);
      Assert.Null(exception);
      Assert.Equal(DeviceConnectionState.Connected, controller.ConnectionState);

      // The exception will be thrown after the device initialization and will be caught by the auto-updater.
      // The device should remain in the "connected" state after exception is caught.
      do
        await Task.Delay(StateCheckPeriod);
      while (!isExceptionCaught);
      Assert.Equal(controller, eventSender);
      Assert.Equal(TestMessageDevice.TestExceptionText, exception?.Message);
      Assert.Equal(DeviceConnectionState.Connected, controller.ConnectionState);
    }

    /// <summary>
    ///   Testing the repeated controller object disposal.
    /// </summary>
    [Fact]
    public void RepeatedDisposalTest()
    {
      using var controller = new VisaDeviceController();
      controller.Dispose();
    }

    // TODO: Add tests for auto-updater control and names localization.
  }
}
