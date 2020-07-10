using System;
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
      await using var device = new MessageDevice(TestResourceManager.SerialTestDeviceResourceName,
        TestResourceManager.DefaultConnectionTimeout, ResourceManager);
      Assert.Null(device.Session);
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
      await using (var device = new MessageDevice(TestResourceManager.CustomTestDeviceResourceName,
        TestResourceManager.DefaultConnectionTimeout, ResourceManager))
        await Assert.ThrowsAnyAsync<VisaDeviceException>(device.OpenSessionAsync);

      // Testing the supported interface type but with the non-message-based session type.
      await using (var device = new MessageDevice(TestResourceManager.VxiTestDeviceResourceName,
        TestResourceManager.DefaultConnectionTimeout, ResourceManager))
        await Assert.ThrowsAnyAsync<VisaDeviceException>(device.OpenSessionAsync);
    }

    /// <summary>
    ///   Testing the custom test message-based VISA device, derived from the <see cref="MessageDevice" /> class.
    /// </summary>
    [Fact]
    public async Task CustomMessageDeviceTest()
    {
      await using var device = new TestMessageDevice(TestResourceManager.SerialTestDeviceResourceName,
        TestResourceManager.DefaultConnectionTimeout, ResourceManager);
      Assert.Equal(device.TestAsyncProperty, device.AsyncProperties[nameof(device.TestAsyncProperty)]);

      device.ThrowOnInitialization = true;
      await Assert.ThrowsAnyAsync<Exception>(device.OpenSessionAsync);

      device.ThrowOnInitialization = false;
      await device.OpenSessionAsync();

      device.TestAsyncProperty.Setter = int.MaxValue;
      await device.TestAsyncProperty.WaitUntilSetterCompletes();
      await device.TestAsyncProperty.UpdateGetterAsync();
      Assert.Equal(int.MaxValue, device.TestAsyncProperty.Getter);

      device.ThrowOnDeInitialization = true;
      await device.DisposeAsync();
    }
  }
}
