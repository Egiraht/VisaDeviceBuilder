using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ivi.Visa;
using VisaDeviceBuilder.Tests.Components;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="MessageDevice" /> class.
  /// </summary>
  public class MessageDeviceTests
  {
    /// <summary>
    ///   The custom VISA resource manager used for testing purposes.
    /// </summary>
    private TestResourceManager ResourceManager { get; } = new TestResourceManager();

    /// <summary>
    ///   Testing the VISA message-based session opening and closing.
    /// </summary>
    [Fact]
    public async Task MessageBasedSessionTest()
    {
      await using var device = new MessageDevice(TestResourceManager.SerialTestDeviceResourceName, ResourceManager);
      Assert.Null(device.Session);

      // Throw when sending a message with no opened session.
      await Assert.ThrowsAnyAsync<VisaDeviceException>(() => device.SendMessageAsync(string.Empty));

      await device.OpenSessionAsync();
      Assert.IsAssignableFrom<IMessageBasedSession>(device.Session);

      await device.SendMessageAsync(string.Empty);
      await device.CloseSessionAsync();
      Assert.Null(device.Session);
    }

    /// <summary>
    ///   Testing the unsupported VISA resources.
    /// </summary>
    [Fact]
    public async Task UnsupportedResourcesTest()
    {
      // Testing the unsupported interface type.
      await using (var device = new MessageDevice(TestResourceManager.CustomTestDeviceResourceName, ResourceManager))
        await Assert.ThrowsAnyAsync<VisaDeviceException>(device.OpenSessionAsync);

      // Testing the supported interface type but with the non-message-based session type.
      await using (var device = new MessageDevice(TestResourceManager.VxiTestDeviceResourceName, ResourceManager))
        await Assert.ThrowsAnyAsync<VisaDeviceException>(device.OpenSessionAsync);
    }

    /// <summary>
    ///   Testing the custom test message-based VISA device, derived from the <see cref="MessageDevice" /> class.
    /// </summary>
    [Fact]
    public async Task CustomMessageDeviceTest()
    {
      await using var device = new TestMessageDevice(TestResourceManager.SerialTestDeviceResourceName, ResourceManager);
      Assert.Equal(device.TestAsyncProperty, device.AsyncProperties[nameof(device.TestAsyncProperty)]);
      Assert.Equal(device.TestDeviceAction, device.DeviceActions[nameof(device.TestDeviceAction)]);
      Assert.Throws<KeyNotFoundException>(() => device.DeviceActions[nameof(device.InvalidDeviceAction)]);

      device.ThrowOnInitialization = true;
      await Assert.ThrowsAnyAsync<Exception>(device.OpenSessionAsync);

      device.ThrowOnInitialization = false;
      await device.OpenSessionAsync();

      device.TestAsyncProperty.Setter = int.MaxValue;
      await device.TestAsyncProperty.GetSetterProcessingTask();
      await device.TestAsyncProperty.UpdateGetterAsync();
      Assert.Equal(int.MaxValue, device.TestAsyncProperty.Getter);

      // All possible exceptions during the device de-initialization and object disposal must be suppressed.
      device.ThrowOnDeInitialization = true;
      await device.CloseSessionAsync();
    }
  }
}
