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
    ///   Defines the test message.
    /// </summary>
    private const string TestMessage = "Test message\x0A";

    /// <summary>
    ///   Testing new message-based VISA device instance creation.
    /// </summary>
    [Fact]
    public async Task MessageDeviceTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new MessageDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName
      };

      // Checking device properties.
      Assert.Equal(resourceManager, device.ResourceManager);
      Assert.Equal(TestResourceManager.SerialTestDeviceResourceName, device.ResourceName);
      Assert.Equal(MessageDevice.DefaultSupportedMessageBasedInterfaces, device.SupportedInterfaces);
      var resourceNameInfo = device.ResourceNameInfo;
      Assert.NotNull(resourceNameInfo);
      Assert.Equal(TestResourceManager.SerialTestDeviceInterfaceType, resourceNameInfo!.InterfaceType);
      Assert.Equal(TestResourceManager.SerialTestDeviceInterfaceNumber, resourceNameInfo.InterfaceNumber);
      Assert.Equal(TestResourceManager.SerialTestDeviceResourceClass, resourceNameInfo.ResourceClass);
      Assert.Equal(TestResourceManager.SerialTestDeviceResourceName, resourceNameInfo.ExpandedUnaliasedName);
      Assert.Equal(TestResourceManager.SerialTestDeviceResourceName, resourceNameInfo.OriginalResourceName);
      Assert.Equal(TestResourceManager.SerialTestDeviceAliasName, resourceNameInfo.AliasIfExists);
      Assert.Equal(TestResourceManager.SerialTestDeviceAliasName, device.AliasName);
      Assert.Equal(TestResourceManager.SerialTestDeviceAliasName, await device.GetIdentifierAsync());
      Assert.Null(device.Session);
      Assert.False(device.IsSessionOpened);
    }

    /// <summary>
    ///   Testing the VISA message-based session opening and closing.
    /// </summary>
    [Fact]
    public async Task MessageBasedSessionTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new MessageDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName
      };

      // Throw when sending a message with no opened session.
      Assert.Null(device.Session);
      Assert.False(device.IsSessionOpened);
      await Assert.ThrowsAnyAsync<VisaDeviceException>(() => device.SendMessageAsync(string.Empty));

      // Session opening.
      await device.OpenSessionAsync();
      Assert.IsAssignableFrom<IMessageBasedSession>(device.Session);
      Assert.True(device.IsSessionOpened);

      // TestResourceManager must return the sent message back.
      var responseMessage = await device.SendMessageAsync(TestMessage);
      Assert.Equal(TestMessage.TrimEnd('\x0A'), responseMessage);

      // Session closing.
      await device.CloseSessionAsync();
      Assert.Null(device.Session);
      Assert.False(device.IsSessionOpened);
    }

    /// <summary>
    ///   Testing the unsupported VISA resources.
    /// </summary>
    [Fact]
    public async Task UnsupportedResourcesTest()
    {
      // An unsupported interface type should not pass.
      using var resourceManager = new TestResourceManager();
      await using (var device = new MessageDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName
      })
        await Assert.ThrowsAsync<VisaDeviceException>(device.OpenSessionAsync);

      // A supported interface type but with a non-message-based session type also should not pass.
      await using (var device = new MessageDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.VxiTestDeviceResourceName
      })
        await Assert.ThrowsAsync<VisaDeviceException>(device.OpenSessionAsync);
    }
  }
}
