using System;
using System.Threading.Tasks;
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
      var device = new VisaDevice(TestResourceManager.TestDeviceResourceName,
        TestResourceManager.DefaultConnectionTimeout, ResourceManager);
      Assert.Equal(ResourceManager, device.ResourceManager);
      Assert.Equal(TestResourceManager.TestDeviceInterfaceType, device.Interface);
      Assert.Equal(TestResourceManager.DefaultConnectionTimeout, device.ConnectionTimeout);
      Assert.Equal(TestResourceManager.TestDeviceResourceName, device.ResourceName);
      Assert.Equal(TestResourceManager.TestDeviceAliasName, device.AliasName);
      Assert.Equal(TestResourceManager.TestDeviceAliasName, await device.GetIdentifierAsync());
      Assert.Empty(device.AsyncProperties);
      Assert.Equal(DeviceConnectionState.Disconnected, device.DeviceConnectionState);
      Assert.False(device.IsSessionOpened);

      await device.OpenSessionAsync();
      await device.ResetAsync();
      Assert.Equal(DeviceConnectionState.Connected, device.DeviceConnectionState);
      Assert.True(device.IsSessionOpened);

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
      IVisaDevice? devicePointer;
      using (var device = new VisaDevice(TestResourceManager.TestDeviceResourceName,
        TestResourceManager.DefaultConnectionTimeout, ResourceManager))
      {
        devicePointer = device;
        await device.OpenSessionAsync();
        await device.OpenSessionAsync();
      }
      await Assert.ThrowsAsync<ObjectDisposedException>(devicePointer.OpenSessionAsync);
      devicePointer.Dispose();

      await using (var device = new VisaDevice(TestResourceManager.TestDeviceResourceName,
        TestResourceManager.DefaultConnectionTimeout, ResourceManager))
      {
        devicePointer = device;
        await device.CloseSessionAsync();
        await device.CloseSessionAsync();
      }
      await Assert.ThrowsAsync<ObjectDisposedException>(devicePointer.CloseSessionAsync);
      await devicePointer.DisposeAsync();
    }

    /// <inheritdoc />
    public void Dispose() => ResourceManager.Dispose();
  }
}
