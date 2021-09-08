// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ivi.Visa;
using Moq;
using VisaDeviceBuilder.Abstracts;
using VisaDeviceBuilder.Tests.Components;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="VisaDeviceBuilder" /> class.
  /// </summary>
  public class VisaDeviceBuilderTests
  {
    /// <summary>
    ///   Defines the test asynchronous operations delay in milliseconds.
    /// </summary>
    private const int OperationDelay = 1;

    /// <summary>
    ///   Defines the test device connection timeout value.
    /// </summary>
    private const int TestConnectionTimeoutValue = 1234;

    /// <summary>
    ///   Defines the test read-only asynchronous property name.
    /// </summary>
    private const string TestReadOnlyAsyncPropertyName = nameof(TestReadOnlyAsyncPropertyName);

    /// <summary>
    ///   Defines the test write-only asynchronous property name.
    /// </summary>
    private const string TestWriteOnlyAsyncPropertyName = nameof(TestWriteOnlyAsyncPropertyName);

    /// <summary>
    ///   Defines the test read-write asynchronous property name.
    /// </summary>
    private const string TestReadWriteAsyncPropertyName = nameof(TestReadWriteAsyncPropertyName);

    /// <summary>
    ///   Defines the test device action name.
    /// </summary>
    private const string TestDeviceActionName = nameof(TestDeviceActionName);

    /// <summary>
    ///   Defines the test string value.
    /// </summary>
    private const string TestString = "Test string";

    /// <summary>
    ///   Gets or sets the VISA device instance that has called the test device action for the last time.
    /// </summary>
    private IVisaDevice? TestDeviceActionCallingDevice { get; set; }

    /// <summary>
    ///   Gets or sets the VISA device instance that has called the test device initialization callback for the last
    ///   time.
    /// </summary>
    private IVisaDevice? TestInitializeCallbackCallingDevice { get; set; }

    /// <summary>
    ///   Gets or sets the VISA device instance that has called the test device de-initialization callback for the last
    ///   time.
    /// </summary>
    private IVisaDevice? TestDeInitializeCallbackCallingDevice { get; set; }

    /// <summary>
    ///   Gets or sets the VISA device instance that has called the test identifier-getting callback for the last time.
    /// </summary>
    private IVisaDevice? TestGetIdentifierCallbackCallingDevice { get; set; }

    /// <summary>
    ///   Gets or sets the VISA device instance that has called the test device reset callback for the last time.
    /// </summary>
    private IVisaDevice? TestResetCallbackCallingDevice { get; set; }

    /// <summary>
    ///   Defines the callback method for the test device action.
    /// </summary>
    private void TestDeviceActionCallback(IVisaDevice? visaDevice)
    {
      Task.Delay(OperationDelay).Wait();
      TestDeviceActionCallingDevice = visaDevice;
    }

    /// <summary>
    ///   The test device initialization callback.
    /// </summary>
    private void TestInitializeCallback(IVisaDevice? device)
    {
      Task.Delay(OperationDelay).Wait();
      TestInitializeCallbackCallingDevice = device;
    }

    /// <summary>
    ///   The test device de-initialization callback.
    /// </summary>
    private void TestDeInitializeCallback(IVisaDevice? device)
    {
      Task.Delay(OperationDelay).Wait();
      TestDeInitializeCallbackCallingDevice = device;
    }

    /// <summary>
    ///   The test callback that gets the device's identifier.
    /// </summary>
    private string TestGetIdentifierCallback(IVisaDevice? device)
    {
      Task.Delay(OperationDelay).Wait();
      TestGetIdentifierCallbackCallingDevice = device;
      return device?.AliasName ?? string.Empty;
    }

    /// <summary>
    ///   The test callback that resets the device.
    /// </summary>
    private void TestResetCallback(IVisaDevice? device)
    {
      Task.Delay(OperationDelay).Wait();
      TestResetCallbackCallingDevice = device;
    }

    /// <summary>
    ///   Testing VISA resource managers.
    /// </summary>
    [Fact]
    public async Task ResourceManagersTest()
    {
      // Testing the global resource manager.
      // No deep testing is possible because the global resource manager's behavior is highly system-dependant.
      await using var globalResourceManagerDevice = new VisaDeviceBuilder()
        .UseGlobalVisaResourceManager()
        .BuildDevice();
      Assert.Null(globalResourceManagerDevice.ResourceManager);
      Assert.Empty(globalResourceManagerDevice.ResourceName);
      Assert.Equal(VisaDevice.DefaultConnectionTimeout, globalResourceManagerDevice.ConnectionTimeout);

      // Testing a custom resource manager (TestResourceManager).
      await using var testResourceManagerDevice = new VisaDeviceBuilder()
        .UseCustomVisaResourceManagerType<TestResourceManager>()
        .UseDefaultResourceName(TestResourceManager.CustomTestDeviceResourceName)
        .UseConnectionTimeout(TestConnectionTimeoutValue)
        .BuildDevice();
      Assert.IsType<TestResourceManager>(testResourceManagerDevice.ResourceManager);
      Assert.Equal(TestResourceManager.CustomTestDeviceResourceName, testResourceManagerDevice.ResourceName);
      Assert.Equal(TestResourceManager.CustomTestDeviceAliasName, testResourceManagerDevice.AliasName);
      Assert.Equal(TestConnectionTimeoutValue, testResourceManagerDevice.ConnectionTimeout);

      await testResourceManagerDevice.OpenSessionAsync();
      Assert.True(testResourceManagerDevice.IsSessionOpened);
      Assert.Equal(testResourceManagerDevice.AliasName, await testResourceManagerDevice.GetIdentifierAsync());

      await testResourceManagerDevice.CloseSessionAsync();
      Assert.False(testResourceManagerDevice.IsSessionOpened);

      // Testing a wrong resource manager type.
      Assert.Throws<InvalidOperationException>(() =>
        new VisaDeviceBuilder().UseCustomVisaResourceManagerType(typeof(VisaDeviceBuilder)));
    }

    /// <summary>
    ///   Testing hardware interfaces support.
    /// </summary>
    [Fact]
    public async Task HardwareInterfacesSupportTest()
    {
      // Testing default interfaces support.
      await using var defaultInterfacesDevice = new VisaDeviceBuilder()
        .UseCustomVisaResourceManagerType<TestResourceManager>()
        .UseDefaultResourceName(TestResourceManager.CustomTestDeviceResourceName) // Uses a custom interface.
        .UseDefaultSupportedHardwareInterfaces()
        .BuildDevice();
      Assert.Equal(VisaDevice.HardwareInterfaceTypes, defaultInterfacesDevice.SupportedInterfaces);
      Assert.Null(((IBuildableVisaDevice) defaultInterfacesDevice).CustomSupportedInterfaces);

      // Session opening should pass OK because the custom hardware interface type is supported by default by the
      // BuildableVisaDevice type.
      await defaultInterfacesDevice.OpenSessionAsync();
      await defaultInterfacesDevice.CloseSessionAsync();

      // Testing support of only the specified hardware interfaces (serial and USB).
      var hardwareInterfaces = new[] { HardwareInterfaceType.Serial, HardwareInterfaceType.Usb };
      await using var specifiedInterfacesDevice = new VisaDeviceBuilder()
        .UseCustomVisaResourceManagerType<TestResourceManager>()
        .UseDefaultResourceName(TestResourceManager.CustomTestDeviceResourceName) // Uses a custom interface.
        .UseSupportedHardwareInterfaces(hardwareInterfaces)
        .BuildDevice();
      Assert.Equal(hardwareInterfaces, specifiedInterfacesDevice.SupportedInterfaces);
      Assert.Equal(hardwareInterfaces, ((IBuildableVisaDevice) specifiedInterfacesDevice).CustomSupportedInterfaces);

      // Session opening should throw a VisaDeviceException because the custom hardware interface type is not defined
      // as supported.
      await Assert.ThrowsAsync<VisaDeviceException>(specifiedInterfacesDevice.OpenSessionAsync);

      // Changing the device's resource name to the one that supports a serial interface should allow the session
      // opening to pass OK.
      specifiedInterfacesDevice.ResourceName = TestResourceManager.SerialTestDeviceResourceName;
      await defaultInterfacesDevice.OpenSessionAsync();
      await defaultInterfacesDevice.CloseSessionAsync();
    }

    /// <summary>
    ///   Testing addition of a read-only asynchronous property.
    /// </summary>
    [Fact]
    public async Task ReadOnlyAsyncPropertyAdditionTest()
    {
      var testDictionary = new Dictionary<IVisaDevice, string>();
      await using var device = new VisaDeviceBuilder()
        .AddReadOnlyAsyncProperty(TestReadOnlyAsyncPropertyName, visaDevice =>
        {
          Task.Delay(OperationDelay).Wait();
          return testDictionary[visaDevice!];
        })
        .BuildDevice();

      // Accessing the read-only asynchronous property.
      var readOnlyAsyncProperty = device.AsyncProperties.First(asyncProperty =>
        asyncProperty.Name == TestReadOnlyAsyncPropertyName && asyncProperty.CanGet && !asyncProperty.CanSet);
      Assert.Contains(readOnlyAsyncProperty, ((IBuildableVisaDevice) device).CustomAsyncProperties);
      Assert.Same(device, readOnlyAsyncProperty.TargetDevice);
      Assert.Equal(default, readOnlyAsyncProperty.Getter);

      // The getter must return the value from the dictionary.
      testDictionary[device] = TestString;
      readOnlyAsyncProperty.RequestGetterUpdate();
      await readOnlyAsyncProperty.GetGetterUpdatingTask();
      Assert.Equal(TestString, readOnlyAsyncProperty.Getter);

      // The setter must not modify the value in the dictionary.
      testDictionary[device] = string.Empty;
      readOnlyAsyncProperty.Setter = TestString;
      await readOnlyAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(string.Empty, testDictionary[device]);
    }

    /// <summary>
    ///   Testing addition of a write-only asynchronous property.
    /// </summary>
    [Fact]
    public async Task WriteOnlyAsyncPropertyAdditionTest()
    {
      var testDictionary = new Dictionary<IVisaDevice, string>();
      await using var device = new VisaDeviceBuilder()
        .AddWriteOnlyAsyncProperty<string>(TestWriteOnlyAsyncPropertyName, (visaDevice, value) =>
        {
          Task.Delay(OperationDelay).Wait();
          testDictionary[visaDevice!] = value;
        })
        .BuildDevice();

      // Accessing the write-only asynchronous property.
      var writeOnlyAsyncProperty = device.AsyncProperties.First(asyncProperty =>
        asyncProperty.Name == TestWriteOnlyAsyncPropertyName && !asyncProperty.CanGet && asyncProperty.CanSet);
      Assert.Contains(writeOnlyAsyncProperty, ((IBuildableVisaDevice) device).CustomAsyncProperties);
      Assert.Same(device, writeOnlyAsyncProperty.TargetDevice);
      Assert.Equal(default, writeOnlyAsyncProperty.Getter);

      // The getter must return a default value.
      testDictionary[device] = TestString;
      writeOnlyAsyncProperty.RequestGetterUpdate();
      await writeOnlyAsyncProperty.GetGetterUpdatingTask();
      Assert.Equal(default, writeOnlyAsyncProperty.Getter);

      // The setter must modify the value in the dictionary.
      testDictionary[device] = string.Empty;
      writeOnlyAsyncProperty.Setter = TestString;
      await writeOnlyAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestString, testDictionary[device]);
    }

    /// <summary>
    ///   Testing addition of a read-write asynchronous property.
    /// </summary>
    [Fact]
    public async Task ReadWriteAsyncPropertyAdditionTest()
    {
      var testDictionary = new Dictionary<IVisaDevice, string>();
      await using var device = new VisaDeviceBuilder()
        .AddReadWriteAsyncProperty(TestReadWriteAsyncPropertyName,
          visaDevice =>
          {
            Task.Delay(OperationDelay).Wait();
            return testDictionary[visaDevice!];
          },
          (visaDevice, value) =>
          {
            Task.Delay(OperationDelay).Wait();
            testDictionary[visaDevice!] = value;
          })
        .BuildDevice();

      // Accessing the read-write asynchronous property.
      var readWriteAsyncProperty = device.AsyncProperties.First(asyncProperty =>
        asyncProperty.Name == TestReadWriteAsyncPropertyName && asyncProperty.CanGet && asyncProperty.CanSet);
      Assert.Contains(readWriteAsyncProperty, ((IBuildableVisaDevice) device).CustomAsyncProperties);
      Assert.Same(device, readWriteAsyncProperty.TargetDevice);
      Assert.Equal(default, readWriteAsyncProperty.Getter);

      // The getter must return the value from the dictionary.
      testDictionary[device] = TestString;
      readWriteAsyncProperty.RequestGetterUpdate();
      await readWriteAsyncProperty.GetGetterUpdatingTask();
      Assert.Equal(TestString, readWriteAsyncProperty.Getter);

      // The setter must modify the value in the dictionary.
      testDictionary[device] = string.Empty;
      readWriteAsyncProperty.Setter = TestString;
      await readWriteAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestString, testDictionary[device]);
    }

    /// <summary>
    ///   Testing copying of owned asynchronous properties.
    /// </summary>
    [Fact]
    public async Task AsyncPropertiesCopyingTest()
    {
      var testDictionary = new Dictionary<IVisaDevice, string>();
      var readOnlyAsyncProperty = new AsyncProperty<string>(visaDevice =>
        {
          Task.Delay(OperationDelay).Wait();
          return testDictionary[visaDevice!];
        })
        { Name = TestReadOnlyAsyncPropertyName };
      var writeOnlyAsyncProperty = new AsyncProperty<string>((visaDevice, value) =>
        {
          Task.Delay(OperationDelay).Wait();
          testDictionary[visaDevice!] = value;
        })
        { Name = TestWriteOnlyAsyncPropertyName };
      var readWriteAsyncProperty = new AsyncProperty<string>(
          visaDevice =>
          {
            Task.Delay(OperationDelay).Wait();
            return testDictionary[visaDevice!];
          },
          (visaDevice, value) =>
          {
            Task.Delay(OperationDelay).Wait();
            testDictionary[visaDevice!] = value;
          })
        { Name = TestReadWriteAsyncPropertyName };
      await using var device = new VisaDeviceBuilder()
        .CopyAsyncProperties(readOnlyAsyncProperty, writeOnlyAsyncProperty)
        .CopyAsyncProperty(readWriteAsyncProperty)
        .BuildDevice();

      // Accessing the asynchronous properties.
      var copiedReadOnlyAsyncProperty = device.AsyncProperties.First(asyncProperty =>
        asyncProperty.Name == TestReadOnlyAsyncPropertyName && asyncProperty.CanGet && !asyncProperty.CanSet);
      var copiedWriteOnlyAsyncProperty = device.AsyncProperties.First(asyncProperty =>
        asyncProperty.Name == TestWriteOnlyAsyncPropertyName && !asyncProperty.CanGet && asyncProperty.CanSet);
      var copiedReadWriteAsyncProperty = device.AsyncProperties.First(asyncProperty =>
        asyncProperty.Name == TestReadWriteAsyncPropertyName && asyncProperty.CanGet && asyncProperty.CanSet);
      Assert.Contains(copiedReadOnlyAsyncProperty, ((IBuildableVisaDevice) device).CustomAsyncProperties);
      Assert.Contains(copiedWriteOnlyAsyncProperty, ((IBuildableVisaDevice) device).CustomAsyncProperties);
      Assert.Contains(copiedReadWriteAsyncProperty, ((IBuildableVisaDevice) device).CustomAsyncProperties);
      Assert.NotSame(copiedReadOnlyAsyncProperty, readOnlyAsyncProperty);
      Assert.NotSame(copiedWriteOnlyAsyncProperty, writeOnlyAsyncProperty);
      Assert.NotSame(copiedReadWriteAsyncProperty, readWriteAsyncProperty);
      Assert.Same(device, copiedReadOnlyAsyncProperty.TargetDevice);
      Assert.Same(device, copiedWriteOnlyAsyncProperty.TargetDevice);
      Assert.Same(device, copiedReadWriteAsyncProperty.TargetDevice);

      // The readOnlyAsyncProperty's getter must return the value from the dictionary.
      testDictionary[device] = TestString;
      copiedReadOnlyAsyncProperty.RequestGetterUpdate();
      await copiedReadOnlyAsyncProperty.GetGetterUpdatingTask();
      Assert.Equal(TestString, copiedReadOnlyAsyncProperty.Getter);

      // The readOnlyAsyncProperty's setter must modify the value in the dictionary.
      testDictionary[device] = string.Empty;
      copiedWriteOnlyAsyncProperty.Setter = TestString;
      await copiedWriteOnlyAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestString, testDictionary[device]);

      // The readWriteAsyncProperty's getter and setter must return and modify the value in the dictionary respectively.
      testDictionary[device] = string.Empty;
      copiedReadWriteAsyncProperty.Setter = TestString;
      await copiedReadWriteAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestString, testDictionary[device]);
      Assert.Equal(default, copiedReadWriteAsyncProperty.Getter);

      copiedReadWriteAsyncProperty.RequestGetterUpdate();
      await copiedReadWriteAsyncProperty.GetGetterUpdatingTask();
      Assert.Equal(TestString, copiedReadWriteAsyncProperty.Getter);
    }

    /// <summary>
    ///   Testing removing of asynchronous properties.
    /// </summary>
    [Fact]
    public async Task AsyncPropertiesRemovingTest()
    {
      var readWriteAsyncProperty = new AsyncProperty<string>(_ => string.Empty, (_, _) => { })
        { Name = TestReadWriteAsyncPropertyName };
      var deviceBuilder = new VisaDeviceBuilder()
        .AddReadOnlyAsyncProperty(TestReadOnlyAsyncPropertyName,
          _ => string.Empty) // Adding a read-only asynchronous property.
        .AddWriteOnlyAsyncProperty<string>(TestWriteOnlyAsyncPropertyName,
          (_, _) => { }) // Adding a write-only asynchronous property.
        .CopyAsyncProperty(readWriteAsyncProperty); // Adding a read-write asynchronous property.

      // Checking presence of the asynchronous properties in the full device instance.
      await using var fullDevice = deviceBuilder.BuildDevice();
      Assert.Contains(fullDevice.AsyncProperties,
        asyncProperty => asyncProperty.Name == TestReadOnlyAsyncPropertyName);
      Assert.Contains(fullDevice.AsyncProperties,
        asyncProperty => asyncProperty.Name == TestWriteOnlyAsyncPropertyName);
      Assert.Contains(fullDevice.AsyncProperties,
        asyncProperty => asyncProperty.Name == TestReadWriteAsyncPropertyName);

      // Checking presence of the asynchronous properties in the partial device instance with the write-only
      // asynchronous property removed.
      deviceBuilder.RemoveAsyncProperty(TestWriteOnlyAsyncPropertyName);
      deviceBuilder.RemoveAsyncProperty(string.Empty); // Unknown names should be silently rejected.
      await using var partialDevice = deviceBuilder.BuildDevice();
      Assert.Contains(partialDevice.AsyncProperties,
        asyncProperty => asyncProperty.Name == TestReadOnlyAsyncPropertyName);
      Assert.DoesNotContain(partialDevice.AsyncProperties,
        asyncProperty => asyncProperty.Name == TestWriteOnlyAsyncPropertyName);
      Assert.Contains(partialDevice.AsyncProperties,
        asyncProperty => asyncProperty.Name == TestReadWriteAsyncPropertyName);

      // Checking presence of the asynchronous properties in the empty device instance with all asynchronous properties
      // being cleared.
      deviceBuilder.ClearAsyncProperties();
      await using var emptyDevice = deviceBuilder.BuildDevice();
      Assert.Empty(emptyDevice.AsyncProperties);
    }

    /// <summary>
    ///   Testing addition of a device action.
    /// </summary>
    [Fact]
    public async Task DeviceActionAdditionTest()
    {
      await using var device = new VisaDeviceBuilder()
        .AddDeviceAction(TestDeviceActionName, TestDeviceActionCallback)
        .BuildDevice();
      Assert.Null(TestDeviceActionCallingDevice);

      // Accessing the device action.
      var testDeviceAction = device.DeviceActions.First(deviceAction => deviceAction.Name == TestDeviceActionName);
      Assert.Contains(((IBuildableVisaDevice) device).CustomDeviceActions,
        deviceAction => deviceAction.Name == TestDeviceActionName);
      Assert.Same(device, testDeviceAction.TargetDevice);

      // The standard Reset device action must also be present in the device as inherited from the base VisaDevice class.
      Assert.Contains(device.DeviceActions, deviceAction => deviceAction.Name == nameof(IVisaDevice.Reset));

      // The device action must modify the TestDeviceActionCallingDevice property on call.
      await testDeviceAction.ExecuteAsync();
      Assert.Same(device, TestDeviceActionCallingDevice);
    }

    /// <summary>
    ///   Testing copying of owned device actions.
    /// </summary>
    [Fact]
    public async Task DeviceActionCopyingTest()
    {
      // Copying multiple device actions (as well as asynchronous properties) with the same name or even the same
      // instance multiple times is not recommended but is admitted.
      var deviceAction1 = new DeviceAction(TestDeviceActionCallback) { Name = TestDeviceActionName };
      var deviceAction2 = new DeviceAction(TestDeviceActionCallback) { Name = TestDeviceActionName };
      await using var device = new VisaDeviceBuilder()
        .CopyDeviceAction(deviceAction1)
        .CopyDeviceActions(deviceAction2, deviceAction2) // Adding the same instance twice.
        .BuildDevice();

      // Accessing the asynchronous properties.
      var copiedDeviceAction1 = device.DeviceActions
        .Where(deviceAction => deviceAction.Name == TestDeviceActionName)
        .ElementAt(0);
      var copiedDeviceAction2 = device.DeviceActions
        .Where(deviceAction => deviceAction.Name == TestDeviceActionName)
        .ElementAt(1);
      var copiedDeviceAction3 = device.DeviceActions
        .Where(deviceAction => deviceAction.Name == TestDeviceActionName)
        .ElementAt(2);
      Assert.Contains(copiedDeviceAction1, ((IBuildableVisaDevice) device).CustomDeviceActions);
      Assert.Contains(copiedDeviceAction2, ((IBuildableVisaDevice) device).CustomDeviceActions);
      Assert.Contains(copiedDeviceAction3, ((IBuildableVisaDevice) device).CustomDeviceActions);
      Assert.NotSame(deviceAction1, copiedDeviceAction1);
      Assert.NotSame(deviceAction2, copiedDeviceAction2);
      Assert.NotSame(deviceAction2, copiedDeviceAction3);
      Assert.NotSame(copiedDeviceAction1, copiedDeviceAction2);
      Assert.NotSame(copiedDeviceAction2, copiedDeviceAction3);
      Assert.Same(device, copiedDeviceAction1.TargetDevice);
      Assert.Same(device, copiedDeviceAction2.TargetDevice);
      Assert.Same(device, copiedDeviceAction3.TargetDevice);

      // Testing the copied device actions.
      TestDeviceActionCallingDevice = null;
      await copiedDeviceAction1.ExecuteAsync();
      Assert.Same(device, TestDeviceActionCallingDevice);

      TestDeviceActionCallingDevice = null;
      await copiedDeviceAction2.ExecuteAsync();
      Assert.Same(device, TestDeviceActionCallingDevice);

      TestDeviceActionCallingDevice = null;
      await copiedDeviceAction3.ExecuteAsync();
      Assert.Same(device, TestDeviceActionCallingDevice);
    }

    /// <summary>
    ///   Testing removing of device actions.
    /// </summary>
    [Fact]
    public async Task DeviceActionsRemovingTest()
    {
      const string testDeviceActionName1 = TestDeviceActionName + "1";
      const string testDeviceActionName2 = TestDeviceActionName + "2";
      const string testDeviceActionName3 = TestDeviceActionName + "3";
      var deviceAction1 = new DeviceAction(TestDeviceActionCallback) { Name = testDeviceActionName1 };
      var deviceAction2 = new DeviceAction(TestDeviceActionCallback) { Name = testDeviceActionName2 };
      var deviceBuilder = new VisaDeviceBuilder()
        .CopyDeviceActions(deviceAction1, deviceAction2)
        .AddDeviceAction(testDeviceActionName3, TestDeviceActionCallback);

      // Checking presence of the device actions in the full device instance.
      await using var fullDevice = deviceBuilder.BuildDevice();
      Assert.Contains(fullDevice.DeviceActions, deviceAction => deviceAction.Name == testDeviceActionName1);
      Assert.Contains(fullDevice.DeviceActions, deviceAction => deviceAction.Name == testDeviceActionName2);
      Assert.Contains(fullDevice.DeviceActions, deviceAction => deviceAction.Name == testDeviceActionName3);

      // Checking presence of the device actions in the partial device instance with the device action 2 removed.
      deviceBuilder.RemoveDeviceAction(testDeviceActionName2);
      deviceBuilder.RemoveDeviceAction(string.Empty); // Unknown names should be silently rejected.
      await using var partialDevice = deviceBuilder.BuildDevice();
      Assert.Contains(partialDevice.DeviceActions, deviceAction => deviceAction.Name == testDeviceActionName1);
      Assert.DoesNotContain(partialDevice.DeviceActions, deviceAction => deviceAction.Name == testDeviceActionName2);
      Assert.Contains(partialDevice.DeviceActions, deviceAction => deviceAction.Name == testDeviceActionName3);

      // Checking presence of the device actions in the empty device instance with all device actions being cleared.
      // The standard Reset device action must remain in the device because it is inherited from the base VisaDevice
      // class and therefore cannot be removed.
      deviceBuilder.ClearDeviceActions();
      await using var emptyDevice = deviceBuilder.BuildDevice();
      Assert.Contains(emptyDevice.DeviceActions, deviceAction => deviceAction.Name == nameof(IVisaDevice.Reset));
      Assert.DoesNotContain(emptyDevice.DeviceActions, deviceAction => deviceAction.Name == testDeviceActionName1);
      Assert.DoesNotContain(emptyDevice.DeviceActions, deviceAction => deviceAction.Name == testDeviceActionName2);
      Assert.DoesNotContain(emptyDevice.DeviceActions, deviceAction => deviceAction.Name == testDeviceActionName3);
    }

    /// <summary>
    ///   Testing callbacks for the VISA device methods.
    /// </summary>
    [Fact]
    public async Task CallbacksTest()
    {
      await using var device = new VisaDeviceBuilder()
        .UseCustomVisaResourceManagerType<TestResourceManager>()
        .UseDefaultResourceName(TestResourceManager.CustomTestDeviceResourceName)
        .UseInitializeCallback(TestInitializeCallback)
        .UseDeInitializeCallback(TestDeInitializeCallback)
        .UseGetIdentifierCallback(TestGetIdentifierCallback)
        .UseResetCallback(TestResetCallback)
        .BuildDevice();
      Assert.Equal(TestInitializeCallback, ((IBuildableVisaDevice) device).CustomInitializeCallback);
      Assert.Equal(TestDeInitializeCallback, ((IBuildableVisaDevice) device).CustomDeInitializeCallback);
      Assert.Equal(TestGetIdentifierCallback, ((IBuildableVisaDevice) device).CustomGetIdentifierCallback);
      Assert.Equal(TestResetCallback, ((IBuildableVisaDevice) device).CustomResetCallback);
      Assert.Null(TestInitializeCallbackCallingDevice);
      Assert.Null(TestDeInitializeCallbackCallingDevice);
      Assert.Null(TestGetIdentifierCallbackCallingDevice);
      Assert.Null(TestResetCallbackCallingDevice);

      // Testing the device initialization callback.
      await device.OpenSessionAsync();
      Assert.Same(device, TestInitializeCallbackCallingDevice);

      // Getting the device's identifier using a callback.
      var identifier = await device.GetIdentifierAsync();
      Assert.Same(device, TestGetIdentifierCallbackCallingDevice);
      Assert.Equal(device.AliasName, identifier);

      // Testing the reset callback
      await device.ResetAsync();
      Assert.Same(device, TestResetCallbackCallingDevice);

      // Testing the device de-initialization callback.
      await device.CloseSessionAsync();
      Assert.Same(device, TestDeInitializeCallbackCallingDevice);
    }

    /// <summary>
    ///   Testing copying device builder configuration from another buildable device instance using the device builder's
    ///   constructor.
    /// </summary>
    [Fact]
    public async Task DeviceBuilderConfigurationCopyingTest()
    {
      var hardwareInterfaces = new[] { HardwareInterfaceType.Custom };
      // Creating a base device instance without callbacks.
      await using var baseDevice = new VisaDeviceBuilder()
        .UseCustomVisaResourceManagerType<TestResourceManager>()
        .UseDefaultResourceName(TestResourceManager.CustomTestDeviceResourceName)
        .UseConnectionTimeout(TestConnectionTimeoutValue)
        .UseSupportedHardwareInterfaces(hardwareInterfaces)
        .AddReadOnlyAsyncProperty(TestReadOnlyAsyncPropertyName, _ => string.Empty)
        .AddDeviceAction(TestDeviceActionName, TestDeviceActionCallback)
        .BuildDevice();
      Assert.IsType<TestResourceManager>(baseDevice.ResourceManager);
      Assert.Equal(TestResourceManager.CustomTestDeviceResourceName, baseDevice.ResourceName);
      Assert.Equal(TestConnectionTimeoutValue, baseDevice.ConnectionTimeout);
      Assert.Equal(hardwareInterfaces, baseDevice.SupportedInterfaces);
      Assert.Contains(baseDevice.AsyncProperties,
        asyncProperty => asyncProperty.Name == TestReadOnlyAsyncPropertyName);
      Assert.Contains(baseDevice.DeviceActions, deviceAction => deviceAction.Name == TestDeviceActionName);
      Assert.Contains(baseDevice.DeviceActions, deviceAction => deviceAction.Name == nameof(IVisaDevice.Reset));

      // No callbacks are specified for the base device instance and they will not be called.
      await baseDevice.OpenSessionAsync();
      await baseDevice.GetIdentifierAsync();
      await baseDevice.ResetAsync();
      await baseDevice.CloseSessionAsync();
      Assert.Null(TestInitializeCallbackCallingDevice);
      Assert.Null(TestDeInitializeCallbackCallingDevice);
      Assert.Null(TestGetIdentifierCallbackCallingDevice);
      Assert.Null(TestResetCallbackCallingDevice);

      // Copying the configuration from the base buildable device instance to another device builder.
      // Also adding callbacks to it and removing all asynchronous properties and device actions (except Reset).
      var derivedDevice = new VisaDeviceBuilder(baseDevice)
        .ClearAsyncProperties()
        .ClearDeviceActions()
        .UseInitializeCallback(TestInitializeCallback)
        .UseDeInitializeCallback(TestDeInitializeCallback)
        .UseGetIdentifierCallback(TestGetIdentifierCallback)
        .UseResetCallback(TestResetCallback)
        .BuildDevice();
      Assert.NotSame(baseDevice, derivedDevice);
      Assert.IsType<TestResourceManager>(derivedDevice.ResourceManager);
      Assert.Equal(TestResourceManager.CustomTestDeviceResourceName, derivedDevice.ResourceName);
      Assert.Equal(TestConnectionTimeoutValue, derivedDevice.ConnectionTimeout);
      Assert.Equal(hardwareInterfaces, derivedDevice.SupportedInterfaces);
      Assert.Empty(derivedDevice.AsyncProperties);
      Assert.DoesNotContain(derivedDevice.DeviceActions, deviceAction => deviceAction.Name == TestDeviceActionName);
      Assert.Contains(derivedDevice.DeviceActions, deviceAction => deviceAction.Name == nameof(IVisaDevice.Reset));

      // The specified callbacks must be called for the derived device.
      await derivedDevice.OpenSessionAsync();
      await derivedDevice.GetIdentifierAsync();
      await derivedDevice.ResetAsync();
      await derivedDevice.CloseSessionAsync();
      Assert.Same(derivedDevice, TestInitializeCallbackCallingDevice);
      Assert.Same(derivedDevice, TestDeInitializeCallbackCallingDevice);
      Assert.Same(derivedDevice, TestGetIdentifierCallbackCallingDevice);
      Assert.Same(derivedDevice, TestResetCallbackCallingDevice);

      // Testing an exception on trying to copy configuration from an unsupported base device.
      Assert.Throws<InvalidOperationException>(() => new VisaDeviceBuilder(new Mock<IVisaDevice>().Object));
    }

    /// <summary>
    ///   Testing building of a device controller.
    /// </summary>
    [Fact]
    public async Task DeviceControllerTest()
    {
      var hardwareInterfaces = new[] { HardwareInterfaceType.Custom };
      var isTestAsyncPropertyUpdated = false;
      await using var deviceController = new VisaDeviceBuilder()
        .UseCustomVisaResourceManagerType<TestResourceManager>()
        .UseDefaultResourceName(TestResourceManager.CustomTestDeviceResourceName)
        .UseConnectionTimeout(TestConnectionTimeoutValue)
        .UseSupportedHardwareInterfaces(hardwareInterfaces)
        .AddReadOnlyAsyncProperty(TestReadOnlyAsyncPropertyName, _ => isTestAsyncPropertyUpdated = true)
        .AddDeviceAction(TestDeviceActionName, TestDeviceActionCallback)
        .UseInitializeCallback(TestInitializeCallback)
        .UseDeInitializeCallback(TestDeInitializeCallback)
        .UseGetIdentifierCallback(TestGetIdentifierCallback)
        .UseResetCallback(TestResetCallback)
        .BuildDeviceController();
      deviceController.IsAutoUpdaterEnabled = false;

      // Checking the device controller's properties.
      Assert.IsType<TestResourceManager>(deviceController.ResourceManager);
      Assert.Equal(TestResourceManager.CustomTestDeviceResourceName, deviceController.ResourceName);
      Assert.Equal(TestConnectionTimeoutValue, deviceController.Device.ConnectionTimeout);
      Assert.Equal(hardwareInterfaces, deviceController.Device.SupportedInterfaces);
      Assert.Contains(deviceController.AsyncProperties,
        asyncProperty => asyncProperty.Name == TestReadOnlyAsyncPropertyName);
      Assert.Contains(deviceController.DeviceActions, deviceAction => deviceAction.Name == TestDeviceActionName);
      Assert.Contains(deviceController.DeviceActions, deviceAction => deviceAction.Name == nameof(IVisaDevice.Reset));
      Assert.True(deviceController.CanConnect);
      Assert.False(deviceController.IsDeviceReady);
      Assert.False(deviceController.IsMessageDevice);

      // Establishing a device connection and waiting for the device to get ready.
      // The device controller must call the initialization callback, get the device's identifier, and update getters
      // of the device's asynchronous properties.
      deviceController.BeginConnect();
      await deviceController.GetDeviceConnectionTask();
      Assert.True(isTestAsyncPropertyUpdated);
      Assert.Same(deviceController.Device, TestInitializeCallbackCallingDevice);
      Assert.Same(deviceController.Device, TestGetIdentifierCallbackCallingDevice);
      Assert.Null(TestResetCallbackCallingDevice);
      Assert.Null(TestDeInitializeCallbackCallingDevice);
      Assert.False(deviceController.CanConnect);
      Assert.True(deviceController.IsDeviceReady);
      Assert.Equal(deviceController.Device.AliasName, deviceController.Identifier);

      // Updating getters of all the device's asynchronous properties again (i.e. the read-only asynchronous property).
      isTestAsyncPropertyUpdated = false;
      await deviceController.UpdateAsyncPropertiesAsync();
      Assert.True(isTestAsyncPropertyUpdated);

      // Executing all device actions (i.e. Reset and TestDeviceActionCallback).
      foreach (var deviceAction in deviceController.DeviceActions)
        await deviceAction.ExecuteAsync();
      Assert.Same(deviceController.Device, TestResetCallbackCallingDevice);
      Assert.Same(deviceController.Device, TestDeviceActionCallingDevice);
      Assert.Null(TestDeInitializeCallbackCallingDevice);

      deviceController.BeginDisconnect();
      await deviceController.GetDeviceDisconnectionTask();
      Assert.Same(deviceController.Device, TestDeInitializeCallbackCallingDevice);
    }
  }
}
