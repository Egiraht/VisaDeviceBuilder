using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;
using VisaDeviceBuilder.Tests.Components;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="BuildableVisaDevice" /> class.
  /// </summary>
  public class BuildableVisaDeviceTests
  {
    /// <summary>
    ///   Defines the delay in milliseconds for imitation of time-consuming asynchronous operations.
    ///   Must be greater than zero.
    /// </summary>
    private const int OperationDelay = 50;

    /// <summary>
    ///   Defines the test device connection timeout value.
    /// </summary>
    private const int TestConnectionTimeout = 1234;

    /// <summary>
    ///   Defines the test double value.
    /// </summary>
    private const double TestDoubleValue = 4321.1234;

    /// <summary>
    ///   Defines the custom supported hardware interfaces for the test device.
    /// </summary>
    private static readonly HardwareInterfaceType[] CustomSupportedInterfaces =
    {
      HardwareInterfaceType.Custom,
      HardwareInterfaceType.Serial,
      HardwareInterfaceType.Usb
    };

    /// <summary>
    ///   Gets or sets the test value accessible through the test owned asynchronous property.
    /// </summary>
    private double TestValue { get; set; }

    /// <summary>
    ///   Gets the test owned asynchronous property.
    /// </summary>
    private IOwnedAsyncProperty<IVisaDevice, double> TestOwnedAsyncProperty => _testOwnedAsyncProperty ??=
      new OwnedAsyncProperty<IVisaDevice, double>(TestOwnedAsyncPropertyGetter, TestOwnedAsyncPropertySetter)
      {
        Name = nameof(TestOwnedAsyncProperty),
        AutoUpdateGetterAfterSetterCompletes = true
      };
    private IOwnedAsyncProperty<IVisaDevice, double>? _testOwnedAsyncProperty;

    /// <summary>
    ///   Gets the test owned device action.
    /// </summary>
    private IOwnedDeviceAction<IVisaDevice> TestOwnedDeviceAction => _testOwnedDeviceAction ??=
      new OwnedDeviceAction<IVisaDevice>(TestOwnedDeviceActionCallback) {Name = nameof(TestOwnedDeviceAction)};
    private IOwnedDeviceAction<IVisaDevice>? _testOwnedDeviceAction;

    /// <summary>
    ///   The test owned asynchronous property getter callback.
    /// </summary>
    private double TestOwnedAsyncPropertyGetter(IVisaDevice visaDevice)
    {
      Task.Delay(OperationDelay).Wait();
      return TestValue - visaDevice.ConnectionTimeout; // Adding dependency on the owning device instance.
    }

    /// <summary>
    ///   The test owned asynchronous property setter callback.
    /// </summary>
    private void TestOwnedAsyncPropertySetter(IVisaDevice visaDevice, double value)
    {
      Task.Delay(OperationDelay).Wait();
      TestValue = value + visaDevice.ConnectionTimeout; // Adding dependency on the owning device instance.
    }

    /// <summary>
    ///   The test owned device action callback.
    /// </summary>
    private void TestOwnedDeviceActionCallback(IVisaDevice device) => device.Reset();

    /// <summary>
    ///   The test device initialization callback.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private void TestInitializeCallback(IVisaDevice device) => Task.Delay(OperationDelay).Wait();

    /// <summary>
    ///   The test device de-initialization callback.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private void TestDeInitializeCallback(IVisaDevice device) => Task.Delay(OperationDelay).Wait();

    /// <summary>
    ///   The test callback that gets the device's identifier.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private string TestGetIdentifierCallback(IVisaDevice device)
    {
      Task.Delay(OperationDelay).Wait();
      return device.AliasName;
    }

    /// <summary>
    ///   The test device reset callback.
    /// </summary>
    private void TestResetCallback(IVisaDevice device) => Task.Delay(OperationDelay).Wait();

    /// <summary>
    ///   Testing new buildable VISA device instance creation with its custom properties being initialized.
    /// </summary>
    [Fact]
    public void BuildableVisaDeviceTest()
    {
      using var resourceManager = new TestResourceManager();
      using var device = new BuildableVisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName,
        ConnectionTimeout = TestConnectionTimeout,
        CustomSupportedInterfaces = CustomSupportedInterfaces,
        CustomAsyncProperties = {TestOwnedAsyncProperty},
        CustomDeviceActions = {TestOwnedDeviceAction},
        CustomInitializeCallback = TestInitializeCallback,
        CustomDeInitializeCallback = TestDeInitializeCallback,
        CustomGetIdentifierCallback = TestGetIdentifierCallback,
        CustomResetCallback = TestResetCallback,
        CustomDisposables = {resourceManager}
      };
      var baseDevice = (IVisaDevice) device;

      // Checking custom device properties.
      Assert.Equal(CustomSupportedInterfaces, device.CustomSupportedInterfaces);
      Assert.Contains(TestOwnedAsyncProperty, device.CustomAsyncProperties);
      Assert.Contains(TestOwnedDeviceAction, device.CustomDeviceActions);
      Assert.Equal(TestInitializeCallback, device.CustomInitializeCallback);
      Assert.Equal(TestDeInitializeCallback, device.CustomDeInitializeCallback);
      Assert.Equal(TestGetIdentifierCallback, device.CustomGetIdentifierCallback);
      Assert.Equal(TestResetCallback, device.CustomResetCallback);
      Assert.Contains(resourceManager, device.CustomDisposables);

      // Checking base device properties.
      Assert.Same(resourceManager, baseDevice.ResourceManager);
      Assert.Equal(TestResourceManager.CustomTestDeviceResourceName, baseDevice.ResourceName);
      Assert.Equal(TestConnectionTimeout, baseDevice.ConnectionTimeout);
      Assert.Equal(CustomSupportedInterfaces, baseDevice.SupportedInterfaces);
      Assert.Contains(TestOwnedAsyncProperty, baseDevice.AsyncProperties);
      Assert.Contains(TestOwnedDeviceAction, baseDevice.DeviceActions);

      // The device also must inherit the default Reset device action.
      Assert.Contains(baseDevice.DeviceActions, action => action.DeviceActionDelegate == baseDevice.Reset);
    }

    /// <summary>
    ///   Testing new buildable VISA device instance creation with its custom properties having default values.
    /// </summary>
    [Fact]
    public async Task DefaultBuildableVisaDeviceTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new BuildableVisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName
      };
      var baseDevice = (IVisaDevice) device;

      // Buildable device's properties must fallback to the corresponding base default values when no custom properties
      // are set.
      Assert.Equal(VisaDevice.DefaultConnectionTimeout, baseDevice.ConnectionTimeout);
      Assert.Equal(VisaDevice.DefaultSupportedInterfaces, baseDevice.SupportedInterfaces);
      Assert.Empty(baseDevice.AsyncProperties);
      Assert.Contains(baseDevice.DeviceActions, action => action.DeviceActionDelegate == baseDevice.Reset);

      // A buildable device should work like a basic VisaDevice in its default configuration.
      await device.OpenSessionAsync();
      await device.ResetAsync();
      Assert.Equal(device.AliasName, await device.GetIdentifierAsync());
    }

    /// <summary>
    ///   Testing buildable VISA device custom callbacks.
    /// </summary>
    [Fact]
    public async Task BuildableVisaDeviceCallbacksTest()
    {
      var isInitializeCallbackCalled = false;
      var isDeInitializeCallbackCalled = false;
      var isGetIdentifierCallbackCalled = false;
      var isResetCallbackCalled = false;
      using var resourceManager = new TestResourceManager();
      await using var device = new BuildableVisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName,
        ConnectionTimeout = TestConnectionTimeout,
        CustomSupportedInterfaces = new[] {TestResourceManager.CustomTestDeviceInterfaceType},
        CustomInitializeCallback = visaDevice =>
        {
          Assert.Equal(DeviceConnectionState.Initializing, visaDevice.ConnectionState);
          Assert.Equal(TestResourceManager.CustomTestDeviceAliasName, visaDevice.AliasName);
          Task.Delay(OperationDelay).Wait();
          isInitializeCallbackCalled = true;
        },
        CustomDeInitializeCallback = visaDevice =>
        {
          Assert.Equal(DeviceConnectionState.DeInitializing, visaDevice.ConnectionState);
          Assert.Equal(TestResourceManager.CustomTestDeviceAliasName, visaDevice.AliasName);
          Task.Delay(OperationDelay).Wait();
          isDeInitializeCallbackCalled = true;
        },
        CustomGetIdentifierCallback = visaDevice =>
        {
          Assert.Equal(DeviceConnectionState.Connected, visaDevice.ConnectionState);
          Assert.Equal(TestResourceManager.CustomTestDeviceAliasName, visaDevice.AliasName);
          Task.Delay(OperationDelay).Wait();
          isGetIdentifierCallbackCalled = true;
          return TestResourceManager.CustomTestDeviceAliasName.ToUpper();
        },
        CustomResetCallback = visaDevice =>
        {
          Assert.Equal(DeviceConnectionState.Connected, visaDevice.ConnectionState);
          Assert.Equal(TestResourceManager.CustomTestDeviceAliasName, visaDevice.AliasName);
          Task.Delay(OperationDelay).Wait();
          isResetCallbackCalled = true;
        }
      };
      Assert.False(isInitializeCallbackCalled);
      Assert.False(isDeInitializeCallbackCalled);
      Assert.False(isGetIdentifierCallbackCalled);
      Assert.False(isResetCallbackCalled);

      // Testing device initialization.
      await device.OpenSessionAsync();
      Assert.True(isInitializeCallbackCalled);

      // Getting the device identifier.
      var identifier = await device.GetIdentifierAsync();
      Assert.True(isGetIdentifierCallbackCalled);
      Assert.Equal(TestResourceManager.CustomTestDeviceAliasName.ToUpper(), identifier);

      // Testing device resetting.
      await device.ResetAsync();
      Assert.True(isResetCallbackCalled);

      // Testing device de-initialization.
      await device.CloseSessionAsync();
      Assert.True(isDeInitializeCallbackCalled);
    }

    /// <summary>
    ///   Testing custom asynchronous properties of a buildable VISA device.
    /// </summary>
    [Fact]
    public async Task CustomAsyncPropertiesTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new BuildableVisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName,
        ConnectionTimeout = TestConnectionTimeout,
        CustomAsyncProperties = {TestOwnedAsyncProperty} // TestOwnedAsyncProperty accesses the TestValue property.
      };

      // The test owned asynchronous property must be enumerated and owned by the device.
      var ownedAsyncProperty = device.AsyncProperties
        .First(asyncProperty => asyncProperty.Name == nameof(TestOwnedAsyncProperty));
      Assert.Equal(TestOwnedAsyncProperty, ownedAsyncProperty);
      Assert.Same(device, ((IOwnedAsyncProperty<IVisaDevice>) ownedAsyncProperty).Owner);

      // The TestValue should be modified through the TestOwnedAsyncProperty.
      // The owning device's ConnectionTimeout value is added to the setter value according to the
      // TestOwnedAsyncPropertySetter callback.
      await device.OpenSessionAsync();
      ownedAsyncProperty.Setter = TestDoubleValue;
      Assert.Equal(default, TestValue);
      await ownedAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestDoubleValue + TestConnectionTimeout, TestValue);
      await ownedAsyncProperty.GetGetterUpdatingTask();
      Assert.Equal(TestDoubleValue, (double) ownedAsyncProperty.Getter!);
    }

    /// <summary>
    ///   Testing custom device actions of a buildable VISA device.
    /// </summary>
    [Fact]
    public async Task CustomDeviceActionsTest()
    {
      var isResetCallbackCalled = false;
      using var resourceManager = new TestResourceManager();
      await using var device = new BuildableVisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName,
        ConnectionTimeout = TestConnectionTimeout,
        CustomDeviceActions = {TestOwnedDeviceAction}, // TestOwnedDeviceAction calls the device's Reset method.
        CustomResetCallback = visaDevice =>
        {
          Task.Delay(OperationDelay).Wait();
          isResetCallbackCalled = true;
        }
      };

      // The test owned device action must be enumerated and owned by the device.
      var ownedDeviceAction = device.DeviceActions
        .First(deviceAction => deviceAction.Name == nameof(TestOwnedDeviceAction));
      Assert.Equal(TestOwnedDeviceAction, ownedDeviceAction);
      Assert.Same(device, ((IOwnedDeviceAction<IVisaDevice>) ownedDeviceAction).Owner);

      // The device's Reset method and the corresponding CustomResetCallback should be called according to the
      // TestOwnedDeviceAction callback.
      await device.OpenSessionAsync();
      await ownedDeviceAction.ExecuteAsync();
      Assert.True(isResetCallbackCalled);
    }

    /// <summary>
    ///   Testing exceptions thrown when no VISA session is opened.
    /// </summary>
    [Fact]
    public async Task NoOpenedSessionTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new BuildableVisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName
      };

      // No VISA session is opened.
      Assert.Throws<VisaDeviceException>(device.Reset);
      Assert.Throws<VisaDeviceException>(device.GetIdentifier);
      await Assert.ThrowsAsync<VisaDeviceException>(device.ResetAsync);
      await Assert.ThrowsAsync<VisaDeviceException>(device.GetIdentifierAsync);
    }

    /// <summary>
    ///   Testing VISA device cloning.
    /// </summary>
    [Fact]
    public void BuildableVisaDeviceCloningTest()
    {
      IBuildableVisaDevice<IVisaDevice>? clone;
      using var resourceManager = new TestResourceManager();
      using var device = new BuildableVisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName,
        ConnectionTimeout = TestConnectionTimeout,
        CustomSupportedInterfaces = CustomSupportedInterfaces,
        CustomAsyncProperties = {TestOwnedAsyncProperty},
        CustomDeviceActions = {TestOwnedDeviceAction},
        CustomInitializeCallback = TestInitializeCallback,
        CustomDeInitializeCallback = TestDeInitializeCallback,
        CustomGetIdentifierCallback = TestGetIdentifierCallback,
        CustomResetCallback = TestResetCallback,
        CustomDisposables = {resourceManager}
      };

      // The cloned device should contain the same data but must not reference objects from the original device.
      using (clone = (IBuildableVisaDevice<IVisaDevice>) device.Clone())
      {
        // Checking cloned base device properties.
        Assert.NotSame(device, clone);
        Assert.IsType<BuildableVisaDevice>(clone);
        Assert.NotSame(device.ResourceManager, clone.ResourceManager);
        Assert.IsType(device.ResourceManager.GetType(), clone.ResourceManager);
        Assert.Equal(device.ResourceName, clone.ResourceName);
        Assert.Equal(device.AliasName, clone.AliasName);
        Assert.Equal(TestConnectionTimeout, clone.ConnectionTimeout);
        Assert.Equal(device.SupportedInterfaces.AsEnumerable(), clone.SupportedInterfaces.AsEnumerable());
        Assert.Equal(device.AsyncProperties.Count(), clone.AsyncProperties.Count());
        Assert.Equal(device.DeviceActions.Count(), clone.DeviceActions.Count());

        // Checking cloned custom device properties.
        Assert.Equal(CustomSupportedInterfaces, clone.CustomSupportedInterfaces);
        Assert.Contains(clone.CustomAsyncProperties, ownedAsyncProperty =>
          ownedAsyncProperty.Name == nameof(TestOwnedAsyncProperty) && ownedAsyncProperty.Owner == clone);
        Assert.Contains(clone.CustomDeviceActions, ownedDeviceAction =>
          ownedDeviceAction.Name == nameof(TestOwnedDeviceAction) && ownedDeviceAction.Owner == clone);
        Assert.Equal(TestInitializeCallback, clone.CustomInitializeCallback);
        Assert.Equal(TestDeInitializeCallback, clone.CustomDeInitializeCallback);
        Assert.Equal(TestGetIdentifierCallback, clone.CustomGetIdentifierCallback);
        Assert.Equal(TestResetCallback, clone.CustomResetCallback);
        Assert.Empty(clone.CustomDisposables);

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
    ///   Testing buildable VISA device object disposal.
    /// </summary>
    [Fact]
    public async Task BuildableVisaDeviceDisposalTest()
    {
      IVisaDevice? device;
      var resourceManager = new TestResourceManager();
      await using (device = new BuildableVisaDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.CustomTestDeviceResourceName,
        CustomDisposables = {resourceManager}
      })
      {
        await device.OpenSessionAsync();
        Assert.False(resourceManager.IsDisposed); // A resource manager instance should be intact here.
      }

      // The device instance now should be disposed of.
      Assert.Equal(DeviceConnectionState.Disconnected, device.ConnectionState);
      Assert.False(device.IsSessionOpened);
      Assert.Null(device.Session);
      Assert.Throws<ObjectDisposedException>(device.OpenSession);
      Assert.Throws<ObjectDisposedException>(device.CloseSession);
      await Assert.ThrowsAsync<ObjectDisposedException>(device.OpenSessionAsync);
      await Assert.ThrowsAsync<ObjectDisposedException>(device.CloseSessionAsync);

      // A resource manager instance should also be disposed of.
      Assert.True(resourceManager.IsDisposed);

      // Repeated device disposals should pass OK.
      device.Dispose();
      await device.DisposeAsync();
    }
  }
}