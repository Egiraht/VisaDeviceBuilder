using System;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;
using VisaDeviceBuilder.Tests.Components;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="VisaDevice" /> class.
  /// </summary>
  public class VisaDeviceTests
  {
    /// <summary>
    ///   Defines the test device connection timeout value.
    /// </summary>
    private const int TestConnectionTimeout = 1234;

    /// <summary>
    ///   Testing the VISA session opening and closing.
    /// </summary>
    [Fact]
    public async Task VisaSessionTest()
    {
      using var resourceManager = new TestResourceManager();
      var device = new VisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName,
        ConnectionTimeout = TestConnectionTimeout
      };
      Assert.Equal(resourceManager, device.ResourceManager);
      Assert.Equal(TestResourceManager.CustomTestDeviceResourceName, device.ResourceName);
      Assert.Equal(TestConnectionTimeout, device.ConnectionTimeout);
      Assert.NotNull(device.ResourceNameInfo);
      Assert.Equal(TestResourceManager.CustomTestDeviceInterfaceType, device.ResourceNameInfo!.InterfaceType);
      Assert.Equal(TestResourceManager.CustomTestDeviceAliasName, device.AliasName);
      Assert.Equal(TestResourceManager.CustomTestDeviceAliasName, await device.GetIdentifierAsync());

      // Testing the enumerations of automatically collected asynchronous properties and device actions.
      Assert.Empty(device.AsyncProperties);
      Assert.Contains(device.DeviceActions, deviceAction => (DeviceAction) deviceAction == (Action) device.Reset);
      Assert.DoesNotContain(device.DeviceActions,
        deviceAction => (DeviceAction) deviceAction == (Action) device.OpenSession);
      // TODO: Check collecting of device actions added as IDeviceAction properties.
      // TODO: Check naming of asynchronous properties and device actions after collecting.

      // Checking the connection states.
      Assert.Equal(DeviceConnectionState.Disconnected, device.ConnectionState);
      Assert.False(device.IsSessionOpened);

      await device.OpenSessionAsync();
      await device.OpenSessionAsync();
      await device.ResetAsync();
      Assert.Equal(DeviceConnectionState.Connected, device.ConnectionState);
      Assert.True(device.IsSessionOpened);

      await device.CloseSessionAsync();
      await device.CloseSessionAsync();
      Assert.Equal(DeviceConnectionState.Disconnected, device.ConnectionState);
      Assert.False(device.IsSessionOpened);
    }

    /// <summary>
    ///   Testing the VISA device disposal.
    /// </summary>
    [Fact]
    public async Task DeviceDisposalTest()
    {
      IVisaDevice? deviceReference;
      using var resourceManager = new TestResourceManager();
      await using (var device = new VisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName
      })
      {
        deviceReference = device;
        await device.OpenSessionAsync();
      }
      Assert.False(deviceReference.IsSessionOpened);
      await Assert.ThrowsAsync<ObjectDisposedException>(deviceReference.OpenSessionAsync);
      await Assert.ThrowsAsync<ObjectDisposedException>(deviceReference.CloseSessionAsync);
      deviceReference.Dispose();
    }
  }
}
