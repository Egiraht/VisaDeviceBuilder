using System;
using System.Globalization;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;
using VisaDeviceBuilder.Tests.Components;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="OwnedAsyncProperty{TOwner,TValue}" /> class.
  /// </summary>
  public class OwnedOwnedAsyncPropertyTests
  {
    /// <summary>
    ///   Defines the custom int value for typed getter/setter value testing.
    /// </summary>
    private const int TestValue = 123456;

    /// <summary>
    ///   Defines the test asynchronous property name.
    /// </summary>
    private const string TestName = "Test name";

    /// <summary>
    ///   Gets the test message-based VISA device instance.
    /// </summary>
    private TestMessageDevice Device { get; } = new();

    /// <summary>
    ///   The getter method of int type that imitates time-consuming reading of the remote device parameter.
    /// </summary>
    private int GetterCallback(TestMessageDevice device)
    {
      device.TestAsyncProperty.RequestGetterUpdate();
      device.TestAsyncProperty.GetGetterUpdatingTask().Wait();
      return device.TestAsyncProperty.Getter;
    }

    /// <summary>
    ///   The setter method of int type that imitates time-consuming writing of the remote device parameter.
    /// </summary>
    private void SetterCallback(TestMessageDevice device, int value)
    {
      device.TestAsyncProperty.Setter = value;
      device.TestAsyncProperty.GetSetterProcessingTask().Wait();
    }

    /// <summary>
    ///   Testing the get-only asynchronous property.
    /// </summary>
    [Fact]
    public async Task GetOnlyPropertyTest()
    {
      var ownedAsyncProperty = new OwnedAsyncProperty<TestMessageDevice, int>(GetterCallback)
      {
        Owner = Device,
        Name = TestName,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var baseAsyncProperty = (IOwnedAsyncProperty<TestMessageDevice>) ownedAsyncProperty;
      Assert.Same(Device, ownedAsyncProperty.Owner);
      Assert.True(ownedAsyncProperty.CanGet);
      Assert.False(ownedAsyncProperty.CanSet);
      Assert.Equal(GetterCallback, ownedAsyncProperty.OwnedGetterDelegate);
      ownedAsyncProperty.OwnedSetterDelegate.Invoke(Device, default); // The default setter delegate should pass OK.
      Assert.Equal(default, ownedAsyncProperty.Getter);
      Assert.Equal(typeof(int), baseAsyncProperty.ValueType);
      Assert.Equal(TestName, ownedAsyncProperty.Name);
      Assert.False(ownedAsyncProperty.AutoUpdateGetterAfterSetterCompletes);

      // The getter must return the correct value.
      Device.TestValue = TestValue;
      ownedAsyncProperty.RequestGetterUpdate();
      await ownedAsyncProperty.GetGetterUpdatingTask();
      Assert.Equal(TestValue, ownedAsyncProperty.Getter);
      Assert.Equal(TestValue, baseAsyncProperty.Getter);

      // The setter must not change the value.
      Device.TestValue = default;
      ownedAsyncProperty.Setter = TestValue;
      await ownedAsyncProperty.GetSetterProcessingTask();
      ownedAsyncProperty.RequestGetterUpdate();
      await ownedAsyncProperty.GetGetterUpdatingTask();
      Assert.Equal(default, ownedAsyncProperty.Getter);
      Assert.Equal(default(int), baseAsyncProperty.Getter);
    }

    /// <summary>
    ///   Testing the set-only asynchronous property.
    /// </summary>
    [Fact]
    public async Task SetOnlyPropertyTest()
    {
      var ownedAsyncProperty = new OwnedAsyncProperty<TestMessageDevice, int>(SetterCallback)
      {
        Owner = Device,
        Name = TestName,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var baseAsyncProperty = (IOwnedAsyncProperty<TestMessageDevice>) ownedAsyncProperty;
      Assert.Same(Device, ownedAsyncProperty.Owner);
      Assert.False(ownedAsyncProperty.CanGet);
      Assert.True(ownedAsyncProperty.CanSet);
      Assert.Equal(default,
        ownedAsyncProperty.OwnedGetterDelegate.Invoke(Device)); // The default getter delegate should pass OK.
      Assert.Equal(SetterCallback, ownedAsyncProperty.OwnedSetterDelegate);
      Assert.Equal(default, ownedAsyncProperty.Getter);
      Assert.Equal(typeof(int), baseAsyncProperty.ValueType);
      Assert.Equal(TestName, ownedAsyncProperty.Name);
      Assert.False(ownedAsyncProperty.AutoUpdateGetterAfterSetterCompletes);

      // The getter must return a default value.
      Device.TestValue = TestValue;
      ownedAsyncProperty.RequestGetterUpdate();
      await ownedAsyncProperty.GetGetterUpdatingTask();
      Assert.Equal(default, ownedAsyncProperty.Getter);
      Assert.Equal(default(int), baseAsyncProperty.Getter);

      // The setter must change the value.
      Device.TestValue = default;
      ownedAsyncProperty.Setter = TestValue;
      await ownedAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, Device.TestValue);

      Device.TestValue = default;
      baseAsyncProperty.Setter = TestValue;
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, Device.TestValue);
    }

    /// <summary>
    ///   Testing the get/set asynchronous property.
    /// </summary>
    [Fact]
    public async Task GetSetPropertyTest()
    {
      var ownedAsyncProperty = new OwnedAsyncProperty<TestMessageDevice, int>(GetterCallback, SetterCallback)
      {
        Owner = Device,
        Name = TestName,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var baseAsyncProperty = (IOwnedAsyncProperty<TestMessageDevice>) ownedAsyncProperty;
      Assert.Same(Device, ownedAsyncProperty.Owner);
      Assert.True(ownedAsyncProperty.CanGet);
      Assert.True(ownedAsyncProperty.CanSet);
      Assert.Equal(GetterCallback, ownedAsyncProperty.OwnedGetterDelegate);
      Assert.Equal(SetterCallback, ownedAsyncProperty.OwnedSetterDelegate);
      Assert.Equal(default, ownedAsyncProperty.Getter);
      Assert.Equal(typeof(int), baseAsyncProperty.ValueType);
      Assert.Equal(TestName, ownedAsyncProperty.Name);
      Assert.False(ownedAsyncProperty.AutoUpdateGetterAfterSetterCompletes);

      // The getter must return the correct value.
      Device.TestValue = TestValue;
      ownedAsyncProperty.RequestGetterUpdate();
      await ownedAsyncProperty.GetGetterUpdatingTask();
      Assert.Equal(TestValue, ownedAsyncProperty.Getter);
      Assert.Equal(TestValue, baseAsyncProperty.Getter);

      // The setter must change the value.
      Device.TestValue = default;
      ownedAsyncProperty.Setter = TestValue;
      await ownedAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, Device.TestValue);

      Device.TestValue = default;
      baseAsyncProperty.Setter = TestValue;
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, Device.TestValue);

      // Testing base setter type conversion.
      baseAsyncProperty.Setter = TestValue.ToString(CultureInfo.CurrentCulture);
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, Device.TestValue);
    }

    /// <summary>
    ///   Testing ownership change of an owned asynchronous property.
    /// </summary>
    [Fact]
    public async Task OwnershipChangeTest()
    {
      var device1 = new TestMessageDevice();
      var device2 = new TestMessageDevice();
      var ownedAsyncProperty = new OwnedAsyncProperty<TestMessageDevice, int>(GetterCallback, SetterCallback)
      {
        Owner = device1,
        AutoUpdateGetterAfterSetterCompletes = true
      };
      Assert.Equal(device1, ownedAsyncProperty.Owner);
      Assert.Equal(default, ownedAsyncProperty.Getter);
      Assert.Equal(default, device1.TestValue);
      Assert.Equal(default, device2.TestValue);

      // Testing ownership of the device1, this must not influence the device2.
      ownedAsyncProperty.Setter = TestValue;
      await ownedAsyncProperty.GetSetterProcessingTask();
      await ownedAsyncProperty.GetGetterUpdatingTask();
      Assert.Equal(device1, ownedAsyncProperty.Owner);
      Assert.Equal(TestValue, ownedAsyncProperty.Getter);
      Assert.Equal(TestValue, device1.TestValue);
      Assert.Equal(default, device2.TestValue);

      // Testing ownership of the device2, this must not influence the device1.
      device1.TestValue = default;
      ownedAsyncProperty.Owner = device2;
      ownedAsyncProperty.Setter = TestValue;
      await ownedAsyncProperty.GetSetterProcessingTask();
      await ownedAsyncProperty.GetGetterUpdatingTask();
      Assert.Equal(device2, ownedAsyncProperty.Owner);
      Assert.Equal(TestValue, ownedAsyncProperty.Getter);
      Assert.Equal(default, device1.TestValue);
      Assert.Equal(TestValue, device2.TestValue);
    }

    /// <summary>
    ///   Testing the exception thrown when no owning device is specified.
    /// </summary>
    [Fact]
    public async Task NoOwnerExceptionTest()
    {
      Exception? getterException = null;
      Exception? setterException = null;
      var ownedAsyncProperty = new OwnedAsyncProperty<TestMessageDevice, int>(GetterCallback, SetterCallback)
      {
        Owner = null,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      ownedAsyncProperty.GetterException += (_, e) => getterException = e.Exception;
      ownedAsyncProperty.SetterException += (_, e) => setterException = e.Exception;
      Assert.Null(getterException);
      Assert.Null(setterException);

      // Testing the exception on getter.
      ownedAsyncProperty.RequestGetterUpdate();
      await ownedAsyncProperty.GetGetterUpdatingTask();
      Assert.IsType<InvalidOperationException>(getterException);
      Assert.Null(setterException);

      // Testing the exception on setter.
      getterException = null;
      ownedAsyncProperty.Setter = TestValue;
      await ownedAsyncProperty.GetSetterProcessingTask();
      Assert.IsType<InvalidOperationException>(setterException);
      Assert.Null(getterException);
    }

    /// <summary>
    ///   Testing read-only asynchronous property cloning.
    /// </summary>
    [Fact]
    public async Task ReadOnlyPropertyCloningTest()
    {
      var ownedAsyncProperty = new OwnedAsyncProperty<TestMessageDevice, int>(GetterCallback)
      {
        Owner = Device,
        Name = TestName,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var clone = (OwnedAsyncProperty<TestMessageDevice, int>) ownedAsyncProperty.Clone();
      Assert.Same(Device, clone.Owner);
      Assert.True(clone.CanGet);
      Assert.False(clone.CanSet);
      Assert.Equal(GetterCallback, clone.OwnedGetterDelegate);
      Assert.Equal(default, clone.Getter);
      Assert.Equal(typeof(int), clone.ValueType);
      Assert.Equal(TestName, clone.Name);
      Assert.False(clone.AutoUpdateGetterAfterSetterCompletes);

      // The clone's getter must return the correct value.
      Device.TestValue = TestValue;
      clone.RequestGetterUpdate();
      await clone.GetGetterUpdatingTask();
      Assert.Equal(TestValue, clone.Getter);

      // The clone's setter must not change the value.
      Device.TestValue = default;
      clone.Setter = TestValue;
      await clone.GetSetterProcessingTask();
      Assert.Equal(default, Device.TestValue);
    }

    /// <summary>
    ///   Testing write-only asynchronous property cloning.
    /// </summary>
    [Fact]
    public async Task WriteOnlyPropertyCloningTest()
    {
      var ownedAsyncProperty = new OwnedAsyncProperty<TestMessageDevice, int>(SetterCallback)
      {
        Owner = Device,
        Name = TestName,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var clone = (OwnedAsyncProperty<TestMessageDevice, int>) ownedAsyncProperty.Clone();
      Assert.Same(Device, clone.Owner);
      Assert.False(clone.CanGet);
      Assert.True(clone.CanSet);
      Assert.Equal(SetterCallback, clone.OwnedSetterDelegate);
      Assert.Equal(default, clone.Getter);
      Assert.Equal(typeof(int), clone.ValueType);
      Assert.Equal(TestName, clone.Name);
      Assert.False(clone.AutoUpdateGetterAfterSetterCompletes);

      // The clone's getter must return a default value.
      Device.TestValue = TestValue;
      clone.RequestGetterUpdate();
      await clone.GetGetterUpdatingTask();
      Assert.Equal(default, clone.Getter);

      // The clone's setter must change the value.
      Device.TestValue = default;
      clone.Setter = TestValue;
      await clone.GetSetterProcessingTask();
      Assert.Equal(TestValue, Device.TestValue);
    }

    /// <summary>
    ///   Testing read-write asynchronous property cloning.
    /// </summary>
    [Fact]
    public async Task ReadWritePropertyCloningTest()
    {
      var ownedAsyncProperty = new OwnedAsyncProperty<TestMessageDevice, int>(GetterCallback, SetterCallback)
      {
        Owner = Device,
        Name = TestName,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var clone = (OwnedAsyncProperty<TestMessageDevice, int>) ownedAsyncProperty.Clone();
      Assert.Same(Device, clone.Owner);
      Assert.True(clone.CanGet);
      Assert.True(clone.CanSet);
      Assert.Equal(GetterCallback, clone.OwnedGetterDelegate);
      Assert.Equal(SetterCallback, clone.OwnedSetterDelegate);
      Assert.Equal(default, clone.Getter);
      Assert.Equal(typeof(int), clone.ValueType);
      Assert.Equal(TestName, clone.Name);
      Assert.False(clone.AutoUpdateGetterAfterSetterCompletes);

      // The clone's getter must return the correct value.
      Device.TestValue = TestValue;
      clone.RequestGetterUpdate();
      await clone.GetGetterUpdatingTask();
      Assert.Equal(TestValue, clone.Getter);

      // The clone's setter must change the value.
      Device.TestValue = default;
      clone.Setter = TestValue;
      await clone.GetSetterProcessingTask();
      Assert.Equal(TestValue, Device.TestValue);
    }
  }
}
