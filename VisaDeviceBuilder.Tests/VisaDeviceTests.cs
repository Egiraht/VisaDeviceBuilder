using System;
using System.Linq;
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
    ///   Testing new VISA device instance creation.
    /// </summary>
    [Fact]
    public async Task VisaDeviceTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new VisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName,
        ConnectionTimeout = TestConnectionTimeout
      };

      // Checking device properties.
      Assert.Same(resourceManager, device.ResourceManager);
      Assert.Equal(TestResourceManager.CustomTestDeviceResourceName, device.ResourceName);
      Assert.Equal(TestConnectionTimeout, device.ConnectionTimeout);
      Assert.Equal(VisaDevice.DefaultSupportedInterfaces, device.SupportedInterfaces);
      Assert.Equal(TestResourceManager.CustomTestDeviceAliasName, device.AliasName);
      Assert.Null(device.Session);
      Assert.False(device.IsSessionOpened);

      // Checking resource name parsing.
      var resourceNameInfo = device.ResourceNameInfo;
      Assert.NotNull(resourceNameInfo);
      Assert.Equal(TestResourceManager.CustomTestDeviceInterfaceType, resourceNameInfo!.InterfaceType);
      Assert.Equal(TestResourceManager.CustomTestDeviceInterfaceNumber, resourceNameInfo.InterfaceNumber);
      Assert.Equal(TestResourceManager.CustomTestDeviceResourceClass, resourceNameInfo.ResourceClass);
      Assert.Equal(TestResourceManager.CustomTestDeviceResourceName, resourceNameInfo.ExpandedUnaliasedName);
      Assert.Equal(TestResourceManager.CustomTestDeviceResourceName, resourceNameInfo.OriginalResourceName);
      Assert.Equal(TestResourceManager.CustomTestDeviceAliasName, resourceNameInfo.AliasIfExists);
    }

    /// <summary>
    ///   Testing VISA session opening and closing.
    /// </summary>
    [Fact]
    public async Task VisaSessionTest()
    {
      var isInitializingStatePassed = false;
      var isConnectedStatePassed = false;
      var isDeInitializingStatePassed = false;
      var isDisconnectedStatePassed = false;
      using var resourceManager = new TestResourceManager();
      await using var device = new VisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName
      };
      device.ConnectionStateChanged += (_, state) =>
      {
        if (state == DeviceConnectionState.Initializing)
          isInitializingStatePassed = true;
        else if (state == DeviceConnectionState.Connected)
          isConnectedStatePassed = true;
        else if (state == DeviceConnectionState.DeInitializing)
          isDeInitializingStatePassed = true;
        else if (state == DeviceConnectionState.Disconnected)
          isDisconnectedStatePassed = true;
      };
      Assert.Equal(DeviceConnectionState.Disconnected, device.ConnectionState);
      Assert.False(device.IsSessionOpened);

      // Checking exceptions thrown when no VISA session is opened.
      // Though these methods do nothing by default, these exceptions are required by them intrinsically.
      Assert.Throws<VisaDeviceException>(device.Reset);
      Assert.Throws<VisaDeviceException>(device.GetIdentifier);
      await Assert.ThrowsAsync<VisaDeviceException>(device.ResetAsync);
      await Assert.ThrowsAsync<VisaDeviceException>(device.GetIdentifierAsync);

      // Session opening.
      await device.OpenSessionAsync();
      Assert.True(isInitializingStatePassed);
      Assert.True(isConnectedStatePassed);
      Assert.Equal(DeviceConnectionState.Connected, device.ConnectionState);
      Assert.NotNull(device.Session);
      Assert.True(device.IsSessionOpened);
      await device.OpenSessionAsync(); // Repeated session opening call should pass OK.
      await device.ResetAsync(); // Reset is empty and should pass OK.

      // Testing timeout modification.
      Assert.Equal(VisaDevice.DefaultConnectionTimeout, device.ConnectionTimeout);
      Assert.Equal(VisaDevice.DefaultConnectionTimeout, device.Session!.TimeoutMilliseconds);
      device.ConnectionTimeout = TestConnectionTimeout;
      Assert.Equal(TestConnectionTimeout, device.ConnectionTimeout);
      Assert.Equal(TestConnectionTimeout, device.Session!.TimeoutMilliseconds);

      // Session closing.
      await device.CloseSessionAsync();
      Assert.True(isDeInitializingStatePassed);
      Assert.True(isDisconnectedStatePassed);
      Assert.Equal(DeviceConnectionState.Disconnected, device.ConnectionState);
      Assert.Null(device.Session);
      Assert.False(device.IsSessionOpened);
      await device.CloseSessionAsync(); // Repeated session closing call should pass OK.
    }

    /// <summary>
    ///   Testing the VISA device's asynchronous properties.
    /// </summary>
    [Fact]
    public void VisaAsyncPropertiesTest()
    {
      // VisaDevice instance has no predefined asynchronous properties.
      using (var visaDevice = new VisaDevice())
        Assert.Empty(visaDevice.AsyncProperties);

      // TestMessageDevice must contain the single TestAsyncProperty in its AsyncProperties enumeration.
      using (var messageDevice = new TestMessageDevice())
        Assert.Single(messageDevice.AsyncProperties, messageDevice.TestAsyncProperty);
    }

    /// <summary>
    ///   Testing the VISA device's device actions.
    /// </summary>
    [Fact]
    public async Task VisaDeviceActionsTest()
    {
      // TestMessageDevice must contain the Reset (as inherited from VisaDevice), DecoratedTestDeviceAction, and
      // DeclaredTestDeviceAction device actions.
      // It must not contain any other methods with the Action delegate signature but without DeviceActionAttribute,
      // like OpenSession method.
      await using var messageDevice = new TestMessageDevice();
      Assert.Contains(messageDevice.DeviceActions,
        deviceAction => deviceAction.DeviceActionDelegate == messageDevice.Reset);
      Assert.Contains(messageDevice.DeviceActions,
        deviceAction => deviceAction.DeviceActionDelegate == messageDevice.DecoratedTestDeviceAction);
      Assert.Contains(messageDevice.DeviceActions,
        deviceAction => deviceAction == messageDevice.DeclaredTestDeviceAction);
      Assert.DoesNotContain(messageDevice.DeviceActions,
        deviceAction => deviceAction.DeviceActionDelegate == messageDevice.OpenSession);

      // Testing device actions execution.
      Assert.False(messageDevice.IsResetCalled);
      Assert.False(messageDevice.IsDecoratedTestDeviceActionCalled);
      Assert.False(messageDevice.IsDeclaredTestDeviceActionCalled);
      foreach (var deviceAction in messageDevice.DeviceActions)
        await deviceAction.ExecuteAsync();
      Assert.True(messageDevice.IsResetCalled);
      Assert.True(messageDevice.IsDecoratedTestDeviceActionCalled);
      Assert.True(messageDevice.IsDeclaredTestDeviceActionCalled);
    }

    /// <summary>
    ///   Testing unexpected VISA device resource changes.
    /// </summary>
    [Fact]
    public async Task UnexpectedResourceChangesTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new VisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName
      };
      await device.OpenSessionAsync();

      // VISA resource changes must not be allowed when a session is opened.
      Assert.Throws<VisaDeviceException>(() => device.ResourceManager = resourceManager);
      Assert.Throws<VisaDeviceException>(() => device.ResourceName = TestResourceManager.CustomTestDeviceResourceName);
    }

    /// <summary>
    ///   Testing unknown VISA resource name.
    /// </summary>
    [Fact]
    public async Task UnknownResourceNameTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new VisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = string.Empty
      };

      // Unknown VISA resource name must not be resolved, and session opening must fail.
      Assert.Null(device.ResourceNameInfo);
      await Assert.ThrowsAsync<VisaDeviceException>(device.OpenSessionAsync);
    }

    /// <summary>
    ///   Testing exceptions handling during device session manipulations.
    /// </summary>
    [Fact]
    public async Task SessionExceptionsTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new TestMessageDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName
      };

      // Exceptions during device initialization should be thrown.
      device.ThrowOnInitialization = true;
      await Assert.ThrowsAsync<VisaDeviceException>(device.OpenSessionAsync);
      device.ThrowOnInitialization = false;
      await device.OpenSessionAsync();

      // Exceptions during device de-initialization should be suppressed.
      device.ThrowOnDeInitialization = true;
      await device.CloseSessionAsync();
    }

    /// <summary>
    ///   Testing VISA device cloning.
    /// </summary>
    [Fact]
    public void VisaDeviceCloningTest()
    {
      IVisaDevice? clone;
      using var resourceManager = new TestResourceManager();
      using var device = new VisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName,
        ConnectionTimeout = TestConnectionTimeout
      };

      // The cloned device should contain the same data but must not reference objects from the original device.
      using (clone = (IVisaDevice) device.Clone())
      {
        Assert.NotSame(device, clone);
        Assert.IsType<VisaDevice>(clone);
        Assert.NotSame(device.ResourceManager, clone.ResourceManager);
        Assert.IsType<TestResourceManager>(clone.ResourceManager);
        Assert.Equal(device.ResourceName, clone.ResourceName);
        Assert.Equal(device.AliasName, clone.AliasName);
        Assert.Equal(TestConnectionTimeout, clone.ConnectionTimeout);
        Assert.Equal(device.SupportedInterfaces.AsEnumerable(), clone.SupportedInterfaces.AsEnumerable());
        Assert.Equal(device.AsyncProperties.Count(), clone.AsyncProperties.Count());
        Assert.Equal(device.DeviceActions.Count(), clone.DeviceActions.Count());

        // The cloned resource manager instance should be intact here.
        Assert.False(((TestResourceManager) clone.ResourceManager!).IsDisposed);
      }

      // The cloned instance now should be disposed of. Its cloned resource manager instance should also be disposed of.
      Assert.Throws<ObjectDisposedException>(clone.OpenSession);
      Assert.True(((TestResourceManager) clone.ResourceManager!).IsDisposed);

      // The original resource manager instance should still remain intact.
      Assert.False(((TestResourceManager) device.ResourceManager!).IsDisposed);
    }

    /// <summary>
    ///   Testing VISA device object disposal.
    /// </summary>
    [Fact]
    public async Task VisaDeviceDisposalTest()
    {
      IVisaDevice? device;
      using var resourceManager = new TestResourceManager();
      await using (device = new VisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName
      })
        await device.OpenSessionAsync();

      // The device instance now should be disposed of.
      Assert.Equal(DeviceConnectionState.Disconnected, device.ConnectionState);
      Assert.False(device.IsSessionOpened);
      Assert.Null(device.Session);
      Assert.Throws<ObjectDisposedException>(device.OpenSession);
      Assert.Throws<ObjectDisposedException>(device.CloseSession);
      await Assert.ThrowsAsync<ObjectDisposedException>(device.OpenSessionAsync);
      await Assert.ThrowsAsync<ObjectDisposedException>(device.CloseSessionAsync);

      // Repeated device disposals should pass OK.
      device.Dispose();
      await device.DisposeAsync();
    }
  }
}
