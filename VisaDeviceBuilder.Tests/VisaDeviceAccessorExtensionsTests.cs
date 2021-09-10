// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin <stibiu@yandex.ru>

using System.Linq;
using System.Threading.Tasks;
using Moq;
using VisaDeviceBuilder.Abstracts;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="VisaDeviceAccessorExtensions" /> class.
  /// </summary>
  public class VisaDeviceAccessorExtensionsTests
  {
    /// <summary>
    ///   Gets or sets the test value of double type.
    /// </summary>
    private double DoubleValue { get; set; }

    /// <summary>
    ///   Gets or sets the test value of string type.
    /// </summary>
    private string StringValue { get; set; } = string.Empty;

    /// <summary>
    ///   Gets or sets the test value of object type.
    /// </summary>
    private object? ObjectValue { get; set; }

    /// <summary>
    ///   Gets the <see cref="IVisaDeviceAccessor" /> object for testing the extension methods on.
    /// </summary>
    private IVisaDeviceAccessor VisaDeviceAccessor { get; }

    /// <summary>
    ///   Initializing the test objects.
    /// </summary>
    public VisaDeviceAccessorExtensionsTests()
    {
      VisaDeviceAccessor = new VisaDeviceBuilder()
        // IAsyncProperty<double> "DoubleValue" variant A (read-write)
        .AddReadWriteAsyncProperty(
          name: nameof(DoubleValue),
          getter: _ => DoubleValue,
          setter: (_, value) => DoubleValue = value)

        // IAsyncProperty<object?> "DoubleValue" variant B (read-only)
        .AddReadOnlyAsyncProperty<object?>(
          name: nameof(DoubleValue),
          getter: _ => DoubleValue / 2)

        // IAsyncProperty<string> "StringValue" variant A (read-write)
        .AddReadWriteAsyncProperty(
          name: nameof(StringValue),
          getter: _ => StringValue,
          setter: (_, value) => StringValue = value)

        // IAsyncProperty<object?> "StringValue" variant B (read-only)
        .AddReadOnlyAsyncProperty<object?>(
          name: nameof(StringValue),
          getter: _ => StringValue + StringValue)

        // IAsyncProperty<object?> "ObjectValue" variant A (read-write)
        .AddReadWriteAsyncProperty(
          name: nameof(ObjectValue),
          getter: _ => ObjectValue,
          setter: (_, value) => ObjectValue = value)

        // IAsyncProperty<object?> "ObjectValue" variant B (write-only)
        .AddWriteOnlyAsyncProperty<object?>(
          name: nameof(ObjectValue),
          setter: (_, value) => ObjectValue = value?.ToString())

        // IDeviceAction "DoubleValue" variant A
        .AddDeviceAction(
          name: nameof(DoubleValue),
          deviceAction: _ => DoubleValue = double.NaN)

        // IDeviceAction "DoubleValue" variant B
        .AddDeviceAction(
          name: nameof(DoubleValue),
          deviceAction: _ => DoubleValue = double.PositiveInfinity)

        // IDeviceAction "StringValue" variant A
        .AddDeviceAction(
          name: nameof(StringValue),
          deviceAction: _ => StringValue = nameof(DoubleValue))

        // IDeviceAction "StringValue" variant B
        .AddDeviceAction(
          name: nameof(StringValue),
          deviceAction: _ => StringValue = nameof(ObjectValue))

        // IDeviceAction "ObjectValue" variant A
        .AddDeviceAction(
          name: nameof(ObjectValue),
          deviceAction: _ => ObjectValue = double.NaN)

        // IDeviceAction "ObjectValue" variant B
        .AddDeviceAction(
          name: nameof(ObjectValue),
          deviceAction: _ => ObjectValue = nameof(StringValue))

        // Building the final device.
        .BuildDevice();
    }

    /// <summary>
    ///   Testing searching of all asynchronous properties matching the name only.
    /// </summary>
    [Fact]
    public void FindAsyncPropertiesByNameTest()
    {
      var doubleAsyncProperties = VisaDeviceAccessor.FindAsyncProperties(nameof(DoubleValue)).ToList();
      var stringAsyncProperties = VisaDeviceAccessor.FindAsyncProperties(nameof(StringValue)).ToList();
      var objectAsyncProperties = VisaDeviceAccessor.FindAsyncProperties(nameof(ObjectValue)).ToList();
      var mismatchedNameAsyncProperties = VisaDeviceAccessor.FindAsyncProperties(nameof(VisaDeviceAccessor)).ToList();

      // doubleAsyncProperties must contain two asynchronous properties matching the "DoubleValue" name (i.e. both
      // variants).
      Assert.Collection(doubleAsyncProperties,
        asyncProperty => Assert.True(asyncProperty is IAsyncProperty<double>
        {
          Name: nameof(DoubleValue),
          CanGet: true,
          CanSet: true
        }),
        asyncProperty => Assert.True(asyncProperty is IAsyncProperty<object?>
        {
          Name: nameof(DoubleValue),
          CanGet: true,
          CanSet: false
        }));

      // stringAsyncProperties must contain two asynchronous properties matching the "StringValue" name (i.e. both
      // variants).
      Assert.Collection(stringAsyncProperties,
        asyncProperty => Assert.True(asyncProperty is IAsyncProperty<string>
        {
          Name: nameof(StringValue),
          CanGet: true,
          CanSet: true
        }),
        asyncProperty => Assert.True(asyncProperty is IAsyncProperty<object?>
        {
          Name: nameof(StringValue),
          CanGet: true,
          CanSet: false
        }));

      // objectAsyncProperties must contain two asynchronous properties matching the "ObjectValue" name (i.e. both
      // variants).
      Assert.Collection(objectAsyncProperties,
        asyncProperty => Assert.True(asyncProperty is IAsyncProperty<object?>
        {
          Name: nameof(ObjectValue),
          CanGet: true,
          CanSet: true
        }),
        asyncProperty => Assert.True(asyncProperty is IAsyncProperty<object?>
        {
          Name: nameof(ObjectValue),
          CanGet: false,
          CanSet: true
        }));

      // Enumerations of asynchronous properties with mismatched names must be empty.
      Assert.Empty(mismatchedNameAsyncProperties);
    }

    /// <summary>
    ///   Testing searching of all asynchronous properties matching the name and value type.
    /// </summary>
    [Fact]
    public void FindAsyncPropertiesByNameAndValueTypeTest()
    {
      var doubleAsyncProperties = VisaDeviceAccessor.FindAsyncProperties<double>(nameof(DoubleValue)).ToList();
      var stringAsyncProperties = VisaDeviceAccessor.FindAsyncProperties<string>(nameof(StringValue)).ToList();
      var objectAsyncProperties = VisaDeviceAccessor.FindAsyncProperties<object?>(nameof(ObjectValue)).ToList();
      var mismatchedTypeAsyncProperties = VisaDeviceAccessor.FindAsyncProperties<string>(nameof(DoubleValue)).ToList();
      var mismatchedNameAsyncProperties =
        VisaDeviceAccessor.FindAsyncProperties<double>(nameof(VisaDeviceAccessor)).ToList();

      // doubleAsyncProperties must contain a single asynchronous property matching the "DoubleValue" name and double
      // type (i.e. variant A).
      Assert.Collection(doubleAsyncProperties,
        asyncProperty => Assert.True(asyncProperty is
        {
          Name: nameof(DoubleValue),
          CanGet: true,
          CanSet: true
        }));

      // stringAsyncProperties must contain a single asynchronous property matching the "StringValue" name and string
      // type (i.e. variant A).
      Assert.Collection(stringAsyncProperties,
        asyncProperty => Assert.True(asyncProperty is
        {
          Name: nameof(StringValue),
          CanGet: true,
          CanSet: true
        }));

      // objectAsyncProperties must contain two asynchronous properties matching the "ObjectValue" name and object type
      // (i.e. both variants).
      Assert.Collection(objectAsyncProperties,
        asyncProperty => Assert.True(asyncProperty is
        {
          Name: nameof(ObjectValue),
          CanGet: true,
          CanSet: true
        }),
        asyncProperty => Assert.True(asyncProperty is
        {
          Name: nameof(ObjectValue),
          CanGet: false,
          CanSet: true
        }));

      // Enumerations of asynchronous properties with mismatched value types must be empty.
      Assert.Empty(mismatchedTypeAsyncProperties);

      // Enumerations of asynchronous properties with mismatched names must be empty.
      Assert.Empty(mismatchedNameAsyncProperties);
    }

    /// <summary>
    ///   Testing searching of a single asynchronous property matching the name only.
    /// </summary>
    [Fact]
    public void FindAsyncPropertyByNameTest()
    {
      var doubleAsyncProperty = VisaDeviceAccessor.FindAsyncProperty(nameof(DoubleValue));
      var stringAsyncProperty = VisaDeviceAccessor.FindAsyncProperty(nameof(StringValue));
      var objectAsyncProperty = VisaDeviceAccessor.FindAsyncProperty(nameof(ObjectValue));
      var mismatchedNameAsyncProperty = VisaDeviceAccessor.FindAsyncProperty(nameof(VisaDeviceAccessor));

      // doubleAsyncProperty must be the last asynchronous property matching the "DoubleValue" name (i.e. variant B).
      Assert.True(doubleAsyncProperty is IAsyncProperty<object?>
      {
        Name: nameof(DoubleValue),
        CanGet: true,
        CanSet: false
      });

      // stringAsyncProperty must be the last asynchronous property matching the "StringValue" name (i.e. variant B).
      Assert.True(stringAsyncProperty is IAsyncProperty<object?>
      {
        Name: nameof(StringValue),
        CanGet: true,
        CanSet: false
      });

      // objectAsyncProperty must be the last asynchronous property matching the "ObjectValue" name (i.e. variant B).
      Assert.True(objectAsyncProperty is IAsyncProperty<object?>
      {
        Name: nameof(ObjectValue),
        CanGet: false,
        CanSet: true
      });

      // Results for asynchronous properties with mismatched names must be null.
      Assert.Null(mismatchedNameAsyncProperty);
    }

    /// <summary>
    ///   Testing searching of a single asynchronous property matching the name and value type.
    /// </summary>
    [Fact]
    public void FindAsyncPropertyByNameAndValueTypeTest()
    {
      var doubleAsyncProperty = VisaDeviceAccessor.FindAsyncProperty<double>(nameof(DoubleValue));
      var stringAsyncProperty = VisaDeviceAccessor.FindAsyncProperty<string>(nameof(StringValue));
      var objectAsyncProperty = VisaDeviceAccessor.FindAsyncProperty<object?>(nameof(ObjectValue));
      var mismatchedTypeAsyncProperty = VisaDeviceAccessor.FindAsyncProperty<double>(nameof(VisaDeviceAccessor));
      var mismatchedNameAsyncProperty = VisaDeviceAccessor.FindAsyncProperty<double>(nameof(VisaDeviceAccessor));

      // doubleAsyncProperty must be the last asynchronous property matching the "DoubleValue" name and double type
      // (i.e. variant A).
      Assert.True(doubleAsyncProperty is
      {
        Name: nameof(DoubleValue),
        CanGet: true,
        CanSet: true
      });

      // stringAsyncProperty must be the last asynchronous property matching the "StringValue" name and string type
      // (i.e. variant A).
      Assert.True(stringAsyncProperty is
      {
        Name: nameof(StringValue),
        CanGet: true,
        CanSet: true
      });

      // objectAsyncProperty must be the last asynchronous property matching the "ObjectValue" name and object type
      // (i.e. variant B).
      Assert.True(objectAsyncProperty is
      {
        Name: nameof(ObjectValue),
        CanGet: false,
        CanSet: true
      });

      // Results for asynchronous properties with mismatched value types must be empty.
      Assert.Null(mismatchedTypeAsyncProperty);

      // Results for asynchronous properties with mismatched names must be empty.
      Assert.Null(mismatchedNameAsyncProperty);
    }

    /// <summary>
    ///   Testing searching of a single asynchronous property matching the name and value type, and getting its value.
    /// </summary>
    [Fact]
    public async Task GetAsyncPropertyValueTest()
    {
      // Initializing the values of the asynchronous properties.
      DoubleValue = double.MaxValue;
      StringValue = nameof(StringValue);
      ObjectValue = int.MinValue;
      foreach (var asyncProperty in VisaDeviceAccessor.Device.AsyncProperties)
      {
        asyncProperty.RequestGetterUpdate();
        await asyncProperty.GetGetterUpdatingTask();
      }

      // Receiving the resulting values.
      var doubleAsyncPropertyValue =
        VisaDeviceAccessor.GetAsyncPropertyValue(nameof(DoubleValue), double.MaxValue);
      var stringAsyncPropertyValue =
        VisaDeviceAccessor.GetAsyncPropertyValue(nameof(StringValue), nameof(StringValue));
      var objectAsyncPropertyValue =
        VisaDeviceAccessor.GetAsyncPropertyValue(nameof(ObjectValue), (object?) int.MaxValue);
      var mismatchedTypeAsyncPropertyValue =
        VisaDeviceAccessor.GetAsyncPropertyValue(nameof(DoubleValue), nameof(DoubleValue));
      var mismatchedNameAsyncPropertyValue =
        VisaDeviceAccessor.GetAsyncPropertyValue(nameof(VisaDeviceAccessor), double.NaN);

      // doubleAsyncPropertyValue must receive the value from the last readable asynchronous property matching the
      // "DoubleValue" name and double type (i.e. variant A).
      Assert.Equal(DoubleValue, doubleAsyncPropertyValue);

      // stringAsyncPropertyValue must receive the value from the last readable asynchronous property matching the
      // "StringValue" name and string type (i.e. variant A).
      Assert.Equal(nameof(StringValue), stringAsyncPropertyValue);

      // objectAsyncPropertyValue must receive the value from the last readable asynchronous property matching the
      // "ObjectValue" name and object type (i.e. variant A).
      Assert.Equal(int.MinValue, objectAsyncPropertyValue);

      // Results for asynchronous properties with mismatched value types must receive a default value from the method's
      // parameter.
      Assert.Equal(nameof(DoubleValue), mismatchedTypeAsyncPropertyValue);

      // Results for asynchronous properties with mismatched names must receive a default value from the method's
      // parameter.
      Assert.Equal(double.NaN, mismatchedNameAsyncPropertyValue);
    }

    /// <summary>
    ///   Testing searching of a single asynchronous property matching the name and value type, and setting its value.
    /// </summary>
    [Fact]
    public async Task SetAsyncPropertyValueByNameAndValueTypeTest()
    {
      // Setting the new values for the asynchronous properties.
      var doubleAsyncPropertyResult =
        VisaDeviceAccessor.SetAsyncPropertyValue(nameof(DoubleValue), double.MaxValue);
      var stringAsyncPropertyResult =
        VisaDeviceAccessor.SetAsyncPropertyValue(nameof(StringValue), nameof(StringValue));
      var objectAsyncPropertyResult =
        VisaDeviceAccessor.SetAsyncPropertyValue(nameof(ObjectValue), (object?) int.MaxValue);
      var mismatchedTypeAsyncPropertyResult =
        VisaDeviceAccessor.SetAsyncPropertyValue(nameof(DoubleValue), nameof(DoubleValue));
      var mismatchedNameAsyncPropertyResult =
        VisaDeviceAccessor.SetAsyncPropertyValue(nameof(VisaDeviceAccessor), double.NaN);

      // Waiting for the setters to be processed.
      foreach (var asyncProperty in VisaDeviceAccessor.Device.AsyncProperties)
        await asyncProperty.GetSetterProcessingTask();

      // The value for the last writable asynchronous property matching the "DoubleValue" name and double type
      // (i.e. variant A) must be successfully set.
      Assert.True(doubleAsyncPropertyResult);
      Assert.Equal(double.MaxValue, DoubleValue);

      // The value for the last writable asynchronous property matching the "DoubleValue" name and string type
      // (i.e. variant A) must be successfully set.
      Assert.True(stringAsyncPropertyResult);
      Assert.Equal(nameof(StringValue), StringValue);

      // The value for the last writable asynchronous property matching the "ObjectValue" name and object type
      // (i.e. variant B) must be successfully set.
      Assert.True(objectAsyncPropertyResult);
      Assert.Equal(int.MaxValue.ToString(), ObjectValue);

      // Results for asynchronous properties with mismatched value types must return false.
      Assert.False(mismatchedTypeAsyncPropertyResult);

      // Results for asynchronous properties with mismatched names must return false.
      Assert.False(mismatchedNameAsyncPropertyResult);
    }

    /// <summary>
    ///   Testing searching of all device actions matching the name.
    /// </summary>
    [Fact]
    public void FindDeviceActionsByNameTest()
    {
      var doubleDeviceActions = VisaDeviceAccessor.FindDeviceActions(nameof(DoubleValue)).ToList();
      var stringDeviceActions = VisaDeviceAccessor.FindDeviceActions(nameof(StringValue)).ToList();
      var objectDeviceActions = VisaDeviceAccessor.FindDeviceActions(nameof(ObjectValue)).ToList();
      var mismatchedNameDeviceActions = VisaDeviceAccessor.FindDeviceActions(nameof(VisaDeviceAccessor)).ToList();

      // doubleDeviceActions must contain two device actions matching the "DoubleValue" name (i.e. both variants).
      Assert.Collection(doubleDeviceActions,
        deviceAction => Assert.True(deviceAction is { Name: nameof(DoubleValue) }),
        deviceAction => Assert.True(deviceAction is { Name: nameof(DoubleValue) }));
      Assert.NotSame(doubleDeviceActions.ElementAt(0), doubleDeviceActions.ElementAt(1));

      // stringDeviceActions must contain two device actions matching the "StringValue" name (i.e. both variants).
      Assert.Collection(stringDeviceActions,
        deviceAction => Assert.True(deviceAction is { Name: nameof(StringValue) }),
        deviceAction => Assert.True(deviceAction is { Name: nameof(StringValue) }));
      Assert.NotSame(stringDeviceActions.ElementAt(0), stringDeviceActions.ElementAt(1));

      // objectDeviceActions must contain two device actions matching the "ObjectValue" name (i.e. both variants).
      Assert.Collection(objectDeviceActions,
        deviceAction => Assert.True(deviceAction is { Name: nameof(ObjectValue) }),
        deviceAction => Assert.True(deviceAction is { Name: nameof(ObjectValue) }));
      Assert.NotSame(objectDeviceActions.ElementAt(0), objectDeviceActions.ElementAt(1));

      // Enumerations of device actions with mismatched names must be empty.
      Assert.Empty(mismatchedNameDeviceActions);
    }

    /// <summary>
    ///   Testing searching of a single device action matching the name.
    /// </summary>
    [Fact]
    public void FindDeviceActionByNameTest()
    {
      var doubleDeviceAction = VisaDeviceAccessor.FindDeviceAction(nameof(DoubleValue));
      var stringDeviceAction = VisaDeviceAccessor.FindDeviceAction(nameof(StringValue));
      var objectDeviceAction = VisaDeviceAccessor.FindDeviceAction(nameof(ObjectValue));
      var mismatchedNameDeviceAction = VisaDeviceAccessor.FindDeviceAction(nameof(VisaDeviceAccessor));

      // doubleDeviceAction must contain the last device action matching the "DoubleValue" name (i.e. variant B).
      Assert.True(doubleDeviceAction is { Name: nameof(DoubleValue) });
      Assert.Same(VisaDeviceAccessor.FindDeviceActions(nameof(DoubleValue)).Last(), doubleDeviceAction);

      // doubleDeviceAction must contain the last device action matching the "StringValue" name (i.e. variant B).
      Assert.True(stringDeviceAction is { Name: nameof(StringValue) });
      Assert.Same(VisaDeviceAccessor.FindDeviceActions(nameof(StringValue)).Last(), stringDeviceAction);

      // objectDeviceAction must contain the last device action matching the "ObjectValue" name (i.e. variant B).
      Assert.True(objectDeviceAction is { Name: nameof(ObjectValue) });
      Assert.Same(VisaDeviceAccessor.FindDeviceActions(nameof(ObjectValue)).Last(), objectDeviceAction);

      // Results for device actions with mismatched names must be null.
      Assert.Null(mismatchedNameDeviceAction);
    }

    /// <summary>
    ///   Testing execution of a single device action matching the name.
    /// </summary>
    [Fact]
    public async Task ExecuteDeviceActionAsyncTest()
    {
      Assert.Equal(default, DoubleValue);
      Assert.Empty(StringValue);
      Assert.Null(ObjectValue);

      // Only the last device actions among those having the same names (i.e. B variants) must be executed.
      var doubleDeviceActionResult = await VisaDeviceAccessor.ExecuteDeviceActionAsync(nameof(DoubleValue));
      var stringDeviceActionResult = await VisaDeviceAccessor.ExecuteDeviceActionAsync(nameof(StringValue));
      var objectDeviceActionResult = await VisaDeviceAccessor.ExecuteDeviceActionAsync(nameof(ObjectValue));
      Assert.True(doubleDeviceActionResult);
      Assert.True(stringDeviceActionResult);
      Assert.True(objectDeviceActionResult);
      Assert.Equal(double.PositiveInfinity, DoubleValue); // Variant B value.
      Assert.Equal(nameof(ObjectValue), StringValue); // Variant B value.
      Assert.Equal(nameof(StringValue), ObjectValue); // Variant B value.

      // Execution of unknown device actions must return false.
      var unknownDeviceActionResult = await VisaDeviceAccessor.ExecuteDeviceActionAsync(nameof(VisaDeviceAccessor));
      Assert.False(unknownDeviceActionResult);

      // Execution of device actions with CanExecute being false must also return false.
      var testDeviceAction = new Mock<IDeviceAction>();
      testDeviceAction
        .SetupGet(deviceAction => deviceAction.Name)
        .Returns(nameof(testDeviceAction)); // Name returns "testDeviceAction".
      testDeviceAction
        .SetupGet(deviceAction => deviceAction.CanExecute)
        .Returns(false); // CanExecute returns false.
      testDeviceAction
        .Setup(deviceAction => deviceAction.Clone())
        .Returns(testDeviceAction.Object); // Clone() returns the same device action instance.
      var testVisaDevice = new VisaDeviceBuilder()
        .CopyDeviceAction(testDeviceAction.Object)
        .BuildDevice();
      Assert.NotNull(testVisaDevice.FindDeviceAction(nameof(testDeviceAction)));
      Assert.False(await testVisaDevice.ExecuteDeviceActionAsync(nameof(testDeviceAction)));
    }
  }
}
