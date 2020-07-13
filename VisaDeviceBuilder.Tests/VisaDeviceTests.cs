using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VisaDeviceBuilder.Tests.Components;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="VisaDevice" /> class.
  /// </summary>
  public class VisaDeviceTests : IDisposable
  {
    /// <summary>
    ///   The custom VISA resource manager used for testing purposes.
    /// </summary>
    private TestResourceManager ResourceManager { get; } = new TestResourceManager();

    /// <summary>
    ///   Testing the VISA session opening and closing.
    /// </summary>
    [Fact]
    public async Task VisaSessionTest()
    {
      var device = new VisaDevice(TestResourceManager.CustomTestDeviceResourceName,
        TestResourceManager.DefaultConnectionTimeout, ResourceManager);
      Assert.Equal(ResourceManager, device.ResourceManager);
      Assert.Equal(TestResourceManager.CustomTestDeviceInterfaceType, device.Interface);
      Assert.Equal(TestResourceManager.DefaultConnectionTimeout, device.ConnectionTimeout);
      Assert.Equal(TestResourceManager.CustomTestDeviceResourceName, device.ResourceName);
      Assert.Equal(TestResourceManager.CustomTestDeviceAliasName, device.AliasName);
      Assert.Equal(TestResourceManager.CustomTestDeviceAliasName, await device.GetIdentifierAsync());

      Assert.Empty(device.AsyncProperties);
      Assert.Equal(device.ResetAsync, device.AsyncActions[nameof(device.ResetAsync)]);
      Assert.Throws<KeyNotFoundException>(() => device.AsyncActions[nameof(device.OpenSessionAsync)]);

      Assert.Equal(DeviceConnectionState.Disconnected, device.DeviceConnectionState);
      Assert.False(device.IsSessionOpened);

      await device.OpenSessionAsync();
      await device.OpenSessionAsync();
      await device.ResetAsync();
      Assert.Equal(DeviceConnectionState.Connected, device.DeviceConnectionState);
      Assert.True(device.IsSessionOpened);

      await device.CloseSessionAsync();
      await device.CloseSessionAsync();
      Assert.Equal(DeviceConnectionState.Disconnected, device.DeviceConnectionState);
      Assert.False(device.IsSessionOpened);
    }

    /// <summary>
    ///   Testing the VISA device disposal.
    /// </summary>
    [Fact]
    public async Task DeviceDisposalTest()
    {
      IVisaDevice? deviceReference;
      await using (var device = new VisaDevice(TestResourceManager.CustomTestDeviceResourceName,
        TestResourceManager.DefaultConnectionTimeout, ResourceManager))
      {
        deviceReference = device;
        await device.OpenSessionAsync();
      }
      Assert.False(deviceReference.IsSessionOpened);
      await Assert.ThrowsAsync<ObjectDisposedException>(deviceReference.OpenSessionAsync);
      await Assert.ThrowsAsync<ObjectDisposedException>(deviceReference.CloseSessionAsync);
      deviceReference.Dispose();
    }

    /// <inheritdoc />
    public void Dispose() => ResourceManager.Dispose();
  }
}
