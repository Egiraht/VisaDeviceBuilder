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
    public const int TestSessionCheckDelay = 200;

    /// <summary>
    ///   Defines the test auto-updater delay value in milliseconds.
    /// </summary>
    public const int TestAutoUpdaterDelay = 123;

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
      Assert.True(controller.IsAutoUpdaterEnabled);
      Assert.Null(controller.LocalizationResourceManager);
      Assert.True(controller.IsMessageDevice);
      Assert.True(controller.CanConnect);
      Assert.Equal(DeviceConnectionState.Disconnected, controller.ConnectionState);
      Assert.Null(controller.Device);
      Assert.Empty(controller.Identifier);
      Assert.False(controller.IsDeviceReady);
      Assert.False(controller.IsUpdatingAsyncProperties);
      Assert.False(controller.IsDisconnectionRequested);

      controller.Connect();
      controller.Connect(); // Repeated call.
      Assert.False(controller.CanConnect);

      // Testing the long connection session.
      await Task.Delay(TestSessionCheckDelay);
      Assert.Equal(DeviceConnectionState.Connected, controller.ConnectionState);
      Assert.NotNull(controller.Device);
      Assert.Equal(TestResourceManager.SerialTestDeviceAliasName, controller.Identifier);
      Assert.True(controller.IsDeviceReady);
      Assert.False(controller.IsDisconnectionRequested);

      var disconnectionTask = controller.DisconnectAsync();
      _ = controller.DisconnectAsync(); // Repeated simultaneous call.
      Assert.True(controller.IsDisconnectionRequested);
      await disconnectionTask;
      Assert.Null(controller.Device);
      Assert.Empty(controller.Identifier);
      Assert.Equal(DeviceConnectionState.Disconnected, controller.ConnectionState);
      Assert.True(controller.CanConnect);
      Assert.False(controller.IsDeviceReady);
      Assert.False(controller.IsUpdatingAsyncProperties);
      Assert.False(controller.IsDisconnectionRequested);

      // Testing the shortest connection session with disconnection on the controller disposal.
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
      await Task.Delay(TestSessionCheckDelay);
      updateAsyncPropertiesTask = controller.UpdateAsyncPropertiesAsync();
      _ = controller.UpdateAsyncPropertiesAsync(); // Repeated simultaneous call.
      Assert.True(controller.IsUpdatingAsyncProperties);
      await updateAsyncPropertiesTask;
      Assert.False(controller.IsUpdatingAsyncProperties);
    }

    // TODO: Add test for localization.

    // TODO: Add test for exceptions.

    /// <summary>
    ///   Testing the repeated controller object disposal.
    /// </summary>
    [Fact]
    public void RepeatedDisposalTest()
    {
      using var controller = new VisaDeviceController();
      controller.Dispose();
    }
  }
}
