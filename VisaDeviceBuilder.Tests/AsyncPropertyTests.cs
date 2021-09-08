// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;
using VisaDeviceBuilder.Tests.Components;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="AsyncProperty{TValue}" /> class.
  /// </summary>
  public class AsyncPropertyTests
  {
    /// <summary>
    ///   Defines the delay in milliseconds for imitation of time-consuming asynchronous operations.
    ///   Must be greater than zero.
    /// </summary>
    private const int OperationDelay = 1;

    /// <summary>
    ///   Defines the custom double value for typed getter/setter value testing.
    /// </summary>
    private const double TestValue = Math.PI;

    /// <summary>
    ///   Defines the test asynchronous property name.
    /// </summary>
    private const string TestName = "Test name";

    /// <summary>
    ///   Defines the custom text message for getter exception testing.
    /// </summary>
    private const string GetterExceptionMessage = "Getter exception";

    /// <summary>
    ///   Defines the custom text message for setter exception testing.
    /// </summary>
    private const string SetterExceptionMessage = "Setter exception";

    /// <summary>
    ///   Gets the test VISA device instance.
    /// </summary>
    private IVisaDevice TestVisaDevice { get; } = new TestMessageDevice();

    /// <summary>
    ///   Gets the dictionary that holds independent values for the test asynchronous property depending on the
    ///   property's target device.
    /// </summary>
    private Dictionary<IVisaDevice, double> PropertyValues { get; } = new();

    /// <summary>
    ///   The getter method of double type that imitates time-consuming reading of the remote device parameter.
    /// </summary>
    private double GetterCallback(IVisaDevice? visaDevice)
    {
      Task.Delay(OperationDelay).Wait();
      return PropertyValues.TryGetValue(visaDevice!, out var value) ? value : default;
    }

    /// <summary>
    ///   The setter method of double type that imitates time-consuming writing of the remote device parameter.
    /// </summary>
    private void SetterCallback(IVisaDevice? visaDevice, double value)
    {
      Task.Delay(OperationDelay).Wait();
      PropertyValues[visaDevice!] = value;
    }

    /// <summary>
    ///   Testing the get-only asynchronous property.
    /// </summary>
    [Fact]
    public async Task ReadOnlyPropertyTest()
    {
      var asyncProperty = new AsyncProperty<double>(GetterCallback)
      {
        Name = TestName,
        TargetDevice = TestVisaDevice,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var baseAsyncProperty = (IAsyncProperty) asyncProperty;
      Assert.True(asyncProperty.CanGet);
      Assert.False(asyncProperty.CanSet);
      Assert.Equal(GetterCallback, asyncProperty.GetterDelegate);
      asyncProperty.SetterDelegate.Invoke(null, default); // The default setter delegate should pass OK.
      Assert.Equal(default, asyncProperty.Getter);
      Assert.Equal(typeof(double), baseAsyncProperty.ValueType);
      Assert.Equal(TestName, asyncProperty.Name);
      Assert.Same(TestVisaDevice, asyncProperty.TargetDevice);
      Assert.False(asyncProperty.AutoUpdateGetterAfterSetterCompletes);

      // The getter must return the correct value.
      PropertyValues[TestVisaDevice] = TestValue;
      asyncProperty.RequestGetterUpdate();
      await asyncProperty.GetGetterUpdatingTask();
      Assert.Equal(TestValue, asyncProperty.Getter);
      Assert.Equal(TestValue, baseAsyncProperty.Getter);

      // The setter must not change the value.
      PropertyValues[TestVisaDevice] = default;
      asyncProperty.Setter = TestValue;
      await asyncProperty.GetSetterProcessingTask();
      asyncProperty.RequestGetterUpdate();
      await asyncProperty.GetGetterUpdatingTask();
      Assert.Equal(default, asyncProperty.Getter);
      Assert.Equal(default(double), baseAsyncProperty.Getter);
    }

    /// <summary>
    ///   Testing the set-only asynchronous property.
    /// </summary>
    [Fact]
    public async Task WriteOnlyPropertyTest()
    {
      var asyncProperty = new AsyncProperty<double>(SetterCallback)
      {
        Name = TestName,
        TargetDevice = TestVisaDevice,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var baseAsyncProperty = (IAsyncProperty) asyncProperty;
      Assert.False(asyncProperty.CanGet);
      Assert.True(asyncProperty.CanSet);
      Assert.Equal(default, asyncProperty.GetterDelegate.Invoke(null)); // The default getter delegate should pass OK.
      Assert.Equal(SetterCallback, asyncProperty.SetterDelegate);
      Assert.Equal(default, asyncProperty.Getter);
      Assert.Equal(typeof(double), baseAsyncProperty.ValueType);
      Assert.Equal(TestName, asyncProperty.Name);
      Assert.Same(TestVisaDevice, asyncProperty.TargetDevice);
      Assert.False(asyncProperty.AutoUpdateGetterAfterSetterCompletes);

      // The getter must return a default value.
      PropertyValues[TestVisaDevice] = TestValue;
      asyncProperty.RequestGetterUpdate();
      await asyncProperty.GetGetterUpdatingTask();
      Assert.Equal(default, asyncProperty.Getter);
      Assert.Equal(default(double), baseAsyncProperty.Getter);

      // The setter must change the value.
      PropertyValues[TestVisaDevice] = default;
      asyncProperty.Setter = TestValue;
      await asyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValues[TestVisaDevice]);

      PropertyValues[TestVisaDevice] = default;
      baseAsyncProperty.Setter = TestValue;
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValues[TestVisaDevice]);
    }

    /// <summary>
    ///   Testing the get/set asynchronous property.
    /// </summary>
    [Fact]
    public async Task ReadWritePropertyTest()
    {
      var asyncProperty = new AsyncProperty<double>(GetterCallback, SetterCallback)
      {
        Name = TestName,
        TargetDevice = TestVisaDevice,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var baseAsyncProperty = (IAsyncProperty) asyncProperty;
      Assert.True(asyncProperty.CanGet);
      Assert.True(asyncProperty.CanSet);
      Assert.Equal(GetterCallback, asyncProperty.GetterDelegate);
      Assert.Equal(SetterCallback, asyncProperty.SetterDelegate);
      Assert.Equal(default, asyncProperty.Getter);
      Assert.Equal(typeof(double), baseAsyncProperty.ValueType);
      Assert.Equal(TestName, asyncProperty.Name);
      Assert.Same(TestVisaDevice, asyncProperty.TargetDevice);
      Assert.False(asyncProperty.AutoUpdateGetterAfterSetterCompletes);

      // The getter must return the correct value.
      PropertyValues[TestVisaDevice] = TestValue;
      asyncProperty.RequestGetterUpdate();
      await asyncProperty.GetGetterUpdatingTask();
      Assert.Equal(TestValue, asyncProperty.Getter);
      Assert.Equal(TestValue, baseAsyncProperty.Getter);

      // The setter must change the value.
      PropertyValues[TestVisaDevice] = default;
      asyncProperty.Setter = TestValue;
      await asyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValues[TestVisaDevice]);

      PropertyValues[TestVisaDevice] = default;
      baseAsyncProperty.Setter = TestValue;
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValues[TestVisaDevice]);

      // Testing base setter type conversion.
      baseAsyncProperty.Setter = TestValue.ToString(CultureInfo.CurrentCulture);
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValues[TestVisaDevice]);
    }

    /// <summary>
    ///   Testing base setter type conversion.
    /// </summary>
    [Fact]
    public async Task BaseSetterTypeConversionTest()
    {
      IAsyncProperty baseAsyncProperty = new AsyncProperty<double>(GetterCallback, SetterCallback)
      {
        TargetDevice = TestVisaDevice,
        AutoUpdateGetterAfterSetterCompletes = false
      };

      // String to double conversion.
      PropertyValues[TestVisaDevice] = default;
      baseAsyncProperty.Setter = TestValue.ToString(CultureInfo.CurrentCulture);
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValues[TestVisaDevice]);

      // Double to double conversion.
      PropertyValues[TestVisaDevice] = default;
      baseAsyncProperty.Setter = TestValue;
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValues[TestVisaDevice]);

      // Incompatible type conversion.
      PropertyValues[TestVisaDevice] = default;
      baseAsyncProperty.Setter = new object();
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(default, PropertyValues[TestVisaDevice]);

      // Null value conversion.
      PropertyValues[TestVisaDevice] = default;
      baseAsyncProperty.Setter = null;
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(default, PropertyValues[TestVisaDevice]);
    }

    /// <summary>
    ///   Testing auto-updating of the getter after setter value processing completes.
    /// </summary>
    [Fact]
    public async Task GetterAutoUpdatingTest()
    {
      var asyncProperty = new AsyncProperty<double>(GetterCallback, SetterCallback)
      {
        TargetDevice = TestVisaDevice,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      Assert.Equal(default, asyncProperty.Getter);

      // Getter auto-update is disabled, the getter value must not get updated automatically.
      PropertyValues[TestVisaDevice] = default;
      asyncProperty.Setter = TestValue;
      await asyncProperty.GetSetterProcessingTask();
      await asyncProperty.GetGetterUpdatingTask();
      Assert.Equal(default, asyncProperty.Getter);

      // Getter auto-update is enabled, the getter value must get updated automatically.
      PropertyValues[TestVisaDevice] = default;
      asyncProperty.AutoUpdateGetterAfterSetterCompletes = true;
      asyncProperty.Setter = TestValue;
      await asyncProperty.GetSetterProcessingTask();
      await asyncProperty.GetGetterUpdatingTask();
      Assert.Equal(TestValue, asyncProperty.Getter);
    }

    /// <summary>
    ///   Testing the getter/setter events in the asynchronous property.
    /// </summary>
    [Fact]
    public async Task PropertyEventsTest()
    {
      var getterPassed = false;
      var setterPassed = false;
      var asyncProperty = new AsyncProperty<double>(GetterCallback, SetterCallback)
      {
        TargetDevice = TestVisaDevice,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      asyncProperty.GetterUpdated += (_, _) => getterPassed = true;
      asyncProperty.SetterCompleted += (_, _) => setterPassed = true;

      // Updating getter, getter updated event must be called, setter completed event must not.
      asyncProperty.RequestGetterUpdate();
      await asyncProperty.GetGetterUpdatingTask();
      Assert.True(getterPassed);
      Assert.False(setterPassed);

      // Changing setter, setter completed event must be called, getter updated event must not.
      getterPassed = false;
      asyncProperty.Setter = TestValue;
      await asyncProperty.GetSetterProcessingTask();
      Assert.False(getterPassed);
      Assert.True(setterPassed);
    }

    /// <summary>
    ///   Testing the exception events in the asynchronous property.
    /// </summary>
    [Fact]
    public async Task PropertyExceptionsTest()
    {
      Exception? getterException = null;
      Exception? setterException = null;
      var asyncProperty = new AsyncProperty<double>(
          _ => throw new Exception(GetterExceptionMessage),
          (_, _) => throw new Exception(SetterExceptionMessage))
      {
        TargetDevice = TestVisaDevice,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      asyncProperty.GetterException += (_, e) => getterException = e.Exception;
      asyncProperty.SetterException += (_, e) => setterException = e.Exception;
      Assert.Null(getterException);
      Assert.Null(setterException);

      // Updating getter, getter exception event must be called, setter exception event must not.
      asyncProperty.RequestGetterUpdate();
      await asyncProperty.GetGetterUpdatingTask();
      Assert.Null(setterException);
      Assert.IsType<Exception>(getterException);
      Assert.Equal(GetterExceptionMessage, getterException!.Message);

      // Changing setter, setter exception event must be called, getter exception event must not.
      getterException = null;
      asyncProperty.Setter = TestValue;
      await asyncProperty.GetSetterProcessingTask();
      Assert.Null(getterException);
      Assert.IsType<Exception>(setterException);
      Assert.Equal(SetterExceptionMessage, setterException!.Message);
    }

    /// <summary>
    ///   Testing read-only asynchronous property cloning.
    /// </summary>
    [Fact]
    public async Task ReadOnlyPropertyCloningTest()
    {
      var asyncProperty = new AsyncProperty<double>(GetterCallback)
      {
        Name = TestName,
        TargetDevice = TestVisaDevice,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var clone = (AsyncProperty<double>) asyncProperty.Clone();
      Assert.True(clone.CanGet);
      Assert.False(clone.CanSet);
      Assert.Equal(GetterCallback, clone.GetterDelegate);
      Assert.Equal(default, clone.Getter);
      Assert.Equal(typeof(double), clone.ValueType);
      Assert.Equal(TestName, clone.Name);
      Assert.Same(TestVisaDevice, clone.TargetDevice);
      Assert.False(clone.AutoUpdateGetterAfterSetterCompletes);

      // The clone's getter must return the correct value.
      PropertyValues[TestVisaDevice] = TestValue;
      clone.RequestGetterUpdate();
      await clone.GetGetterUpdatingTask();
      Assert.Equal(TestValue, clone.Getter);

      // The clone's setter must not change the value.
      PropertyValues[TestVisaDevice] = default;
      clone.Setter = TestValue;
      await clone.GetSetterProcessingTask();
      Assert.Equal(default, PropertyValues[TestVisaDevice]);
    }

    /// <summary>
    ///   Testing write-only asynchronous property cloning.
    /// </summary>
    [Fact]
    public async Task WriteOnlyPropertyCloningTest()
    {
      var asyncProperty = new AsyncProperty<double>(SetterCallback)
      {
        Name = TestName,
        TargetDevice = TestVisaDevice,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var clone = (AsyncProperty<double>) asyncProperty.Clone();
      Assert.False(clone.CanGet);
      Assert.True(clone.CanSet);
      Assert.Equal(SetterCallback, clone.SetterDelegate);
      Assert.Equal(default, clone.Getter);
      Assert.Equal(typeof(double), clone.ValueType);
      Assert.Equal(TestName, clone.Name);
      Assert.Same(TestVisaDevice, clone.TargetDevice);
      Assert.False(clone.AutoUpdateGetterAfterSetterCompletes);

      // The clone's getter must return a default value.
      PropertyValues[TestVisaDevice] = TestValue;
      clone.RequestGetterUpdate();
      await clone.GetGetterUpdatingTask();
      Assert.Equal(default, clone.Getter);

      // The clone's setter must change the value.
      PropertyValues[TestVisaDevice] = default;
      clone.Setter = TestValue;
      await clone.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValues[TestVisaDevice]);
    }

    /// <summary>
    ///   Testing read-write asynchronous property cloning.
    /// </summary>
    [Fact]
    public async Task ReadWritePropertyCloningTest()
    {
      var asyncProperty = new AsyncProperty<double>(GetterCallback, SetterCallback)
      {
        Name = TestName,
        TargetDevice = TestVisaDevice,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var clone = (AsyncProperty<double>) asyncProperty.Clone();
      Assert.True(clone.CanGet);
      Assert.True(clone.CanSet);
      Assert.Equal(GetterCallback, clone.GetterDelegate);
      Assert.Equal(SetterCallback, clone.SetterDelegate);
      Assert.Equal(default, clone.Getter);
      Assert.Equal(typeof(double), clone.ValueType);
      Assert.Equal(TestName, clone.Name);
      Assert.Same(TestVisaDevice, clone.TargetDevice);
      Assert.False(clone.AutoUpdateGetterAfterSetterCompletes);

      // The clone's getter must return the correct value.
      PropertyValues[TestVisaDevice] = TestValue;
      clone.RequestGetterUpdate();
      await clone.GetGetterUpdatingTask();
      Assert.Equal(TestValue, clone.Getter);

      // The clone's setter must change the value.
      PropertyValues[TestVisaDevice] = default;
      clone.Setter = TestValue;
      await clone.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValues[TestVisaDevice]);
    }
  }
}
