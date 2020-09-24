using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  // The extension with additional unit tests covering the typed AsyncProperty class variant.
  public partial class AsyncPropertyTests
  {
    /// <summary>
    ///   The test object class.
    /// </summary>
    private class TestObject
    {
      public string String { get; set; } = string.Empty;
      public double Double { get; set; }
    }

    /// <summary>
    ///   Defines the custom double value for typed getter/setter value testing.
    /// </summary>
    private const double DoubleTestValue = Math.PI;

    /// <summary>
    ///   Gets or sets the double test value imitating a remote device parameter.
    /// </summary>
    private double DoubleRemoteValue { get; set; }

    /// <summary>
    ///   Gets or sets the test object imitating a complex remote device parameter.
    /// </summary>
    private TestObject? RemoteObject { get; set; }

    /// <summary>
    ///   The getter method of double type that imitates time-consuming reading of the remote device parameter.
    /// </summary>
    private double DoubleGetterCallback()
    {
      Task.Delay(OperationDelay).Wait();
      return DoubleRemoteValue;
    }

    /// <summary>
    ///   The setter method of double type that imitates time-consuming writing of the remote device parameter.
    /// </summary>
    private void DoubleSetterCallback(double value)
    {
      Task.Delay(OperationDelay).Wait();
      DoubleRemoteValue = value;
    }

    /// <summary>
    ///   Double to string conversion method.
    /// </summary>
    private static string FromDoubleConverter(double value) =>
      AsyncProperty<double>.DefaultTypeToStringConverter(value);

    /// <summary>
    ///   <see cref="TestObject" /> to string conversion method.
    /// </summary>
    private static string ToTestObjectConverter(TestObject? value) =>
      JsonSerializer.Serialize(value);

    /// <summary>
    ///   String to <see cref="TestObject" /> conversion method.
    /// </summary>
    private static TestObject? FromTestObjectConverter(string value) =>
      JsonSerializer.Deserialize<TestObject?>(value);

    /// <summary>
    ///   Testing the typed get-only asynchronous property.
    /// </summary>
    [Fact]
    public async Task TypedGetOnlyPropertyTest()
    {
      var property = new AsyncProperty<double>(DoubleGetterCallback);
      var baseProperty = (IAsyncProperty) property;
      Assert.True(property.CanGet);
      Assert.False(property.CanSet);
      Assert.Equal(default, property.Getter);
      Assert.Equal(default, property.Setter);
      Assert.Equal(FromDoubleConverter(default), baseProperty.Getter);
      Assert.Equal(FromDoubleConverter(default), baseProperty.Setter);

      DoubleRemoteValue = DoubleTestValue;
      property.Setter = default;
      await property.GetSetterProcessingTask();
      Assert.Equal(DoubleTestValue, DoubleRemoteValue);

      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Equal(DoubleRemoteValue, property.Getter);
      Assert.Equal(FromDoubleConverter(DoubleRemoteValue), baseProperty.Getter);

      baseProperty.Setter = FromDoubleConverter(default);
      await baseProperty.GetSetterProcessingTask();
      Assert.Equal(DoubleTestValue, DoubleRemoteValue);

      baseProperty.RequestGetterUpdate();
      await baseProperty.GetGetterUpdatingTask();
      Assert.Equal(DoubleRemoteValue, property.Getter);
      Assert.Equal(FromDoubleConverter(DoubleRemoteValue), baseProperty.Getter);
    }

    /// <summary>
    ///   Testing the typed set-only asynchronous property.
    /// </summary>
    [Fact]
    public async Task TypedSetOnlyPropertyTest()
    {
      var property = new AsyncProperty<double>(DoubleSetterCallback);
      var baseProperty = (IAsyncProperty) property;
      Assert.False(property.CanGet);
      Assert.True(property.CanSet);
      Assert.Equal(default, property.Getter);
      Assert.Equal(default, property.Setter);
      Assert.Equal(FromDoubleConverter(default), baseProperty.Getter);
      Assert.Equal(FromDoubleConverter(default), baseProperty.Setter);

      DoubleRemoteValue = default;
      property.Setter = DoubleTestValue;
      Assert.Equal(DoubleTestValue, property.Setter);
      Assert.Equal(FromDoubleConverter(DoubleTestValue), baseProperty.Setter);

      await property.GetSetterProcessingTask();
      Assert.Equal(DoubleTestValue, DoubleRemoteValue);
      Assert.Equal(default, property.Setter);
      Assert.Equal(FromDoubleConverter(default), baseProperty.Setter);

      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Equal(default, property.Getter);
      Assert.Equal(FromDoubleConverter(default), baseProperty.Getter);

      DoubleRemoteValue = default;
      baseProperty.Setter = FromDoubleConverter(DoubleTestValue);
      Assert.Equal(DoubleTestValue, property.Setter);
      Assert.Equal(FromDoubleConverter(DoubleTestValue), baseProperty.Setter);

      await baseProperty.GetSetterProcessingTask();
      Assert.Equal(DoubleTestValue, DoubleRemoteValue);
      Assert.Equal(default, property.Setter);
      Assert.Equal(FromDoubleConverter(default), baseProperty.Setter);

      baseProperty.RequestGetterUpdate();
      await baseProperty.GetGetterUpdatingTask();
      Assert.Equal(default, property.Getter);
      Assert.Equal(FromDoubleConverter(default), baseProperty.Getter);
    }

    /// <summary>
    ///   Testing the typed get/set asynchronous property.
    /// </summary>
    [Fact]
    public async Task TypedGetSetPropertyTest()
    {
      var property = new AsyncProperty<double>(DoubleGetterCallback, DoubleSetterCallback);
      var baseProperty = (IAsyncProperty) property;
      Assert.True(property.CanGet);
      Assert.True(property.CanSet);
      Assert.Equal(default, property.Getter);
      Assert.Equal(default, property.Setter);
      Assert.Equal(FromDoubleConverter(default), baseProperty.Getter);
      Assert.Equal(FromDoubleConverter(default), baseProperty.Setter);

      DoubleRemoteValue = default;
      property.Setter = DoubleTestValue;
      Assert.Equal(DoubleTestValue, property.Setter);
      Assert.Equal(FromDoubleConverter(DoubleTestValue), baseProperty.Setter);

      await property.GetSetterProcessingTask();
      Assert.Equal(DoubleTestValue, DoubleRemoteValue);
      Assert.Equal(default, property.Setter);
      Assert.Equal(FromDoubleConverter(default), baseProperty.Setter);

      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Equal(DoubleRemoteValue, property.Getter);
      Assert.Equal(FromDoubleConverter(DoubleRemoteValue), baseProperty.Getter);

      DoubleRemoteValue = default;
      baseProperty.Setter = FromDoubleConverter(DoubleTestValue);
      Assert.Equal(DoubleTestValue, property.Setter);
      Assert.Equal(FromDoubleConverter(DoubleTestValue), baseProperty.Setter);

      await baseProperty.GetSetterProcessingTask();
      Assert.Equal(DoubleTestValue, DoubleRemoteValue);
      Assert.Equal(default, property.Setter);
      Assert.Equal(FromDoubleConverter(default), baseProperty.Setter);

      baseProperty.RequestGetterUpdate();
      await baseProperty.GetGetterUpdatingTask();
      Assert.Equal(DoubleRemoteValue, property.Getter);
      Assert.Equal(FromDoubleConverter(DoubleRemoteValue), baseProperty.Getter);
    }

    /// <summary>
    ///   Testing the value conversion for the typed get-only asynchronous property.
    /// </summary>
    [Fact]
    public async Task TypedGetOnlyPropertyValueConversionTest()
    {
      var property = new AsyncProperty<TestObject?>(() => RemoteObject, ToTestObjectConverter, FromTestObjectConverter);
      var baseProperty = (IAsyncProperty) property;
      RemoteObject = new TestObject
      {
        String = TestValue,
        Double = DoubleTestValue
      };
      var serializedTestObject = JsonSerializer.Serialize(RemoteObject);
      var serializedNullObject = JsonSerializer.Serialize<TestObject?>(null);
      Assert.Equal(serializedNullObject, baseProperty.Getter);
      Assert.Equal(serializedNullObject, baseProperty.Setter);

      property.Setter = null;
      await property.GetSetterProcessingTask();
      Assert.Equal(TestValue, RemoteObject?.String);
      Assert.Equal(DoubleTestValue, RemoteObject?.Double);

      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Equal(RemoteObject?.String, property.Getter?.String);
      Assert.Equal(RemoteObject?.Double, property.Getter?.Double);
      Assert.Equal(serializedTestObject, baseProperty.Getter);

      baseProperty.Setter = serializedNullObject;
      await baseProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, RemoteObject?.String);
      Assert.Equal(DoubleTestValue, RemoteObject?.Double);

      baseProperty.RequestGetterUpdate();
      await baseProperty.GetGetterUpdatingTask();
      Assert.Equal(RemoteObject?.String, property.Getter?.String);
      Assert.Equal(RemoteObject?.Double, property.Getter?.Double);
      Assert.Equal(serializedTestObject, baseProperty.Getter);
    }

    /// <summary>
    ///   Testing the value conversion for the typed set-only asynchronous property.
    /// </summary>
    [Fact]
    public async Task TypedSetOnlyPropertyValueConversionTest()
    {
      var property = new AsyncProperty<TestObject?>(value => RemoteObject = value, ToTestObjectConverter,
        FromTestObjectConverter);
      var baseProperty = (IAsyncProperty) property;
      var newObject = new TestObject
      {
        String = TestValue,
        Double = DoubleTestValue
      };
      var serializedTestObject = JsonSerializer.Serialize(newObject);
      var serializedNullObject = JsonSerializer.Serialize<TestObject?>(null);
      Assert.Equal(serializedNullObject, baseProperty.Getter);
      Assert.Equal(serializedNullObject, baseProperty.Setter);

      RemoteObject = null;
      property.Setter = newObject;
      await property.GetSetterProcessingTask();
      Assert.Equal(newObject.String, RemoteObject?.String);
      Assert.Equal(newObject.Double, RemoteObject?.Double);

      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Equal(default, property.Getter?.String);
      Assert.Equal(default, property.Getter?.Double);
      Assert.Equal(serializedNullObject, baseProperty.Getter);

      RemoteObject = null;
      baseProperty.Setter = serializedTestObject;
      await baseProperty.GetSetterProcessingTask();
      Assert.Equal(newObject.String, RemoteObject?.String);
      Assert.Equal(newObject.Double, RemoteObject?.Double);

      baseProperty.RequestGetterUpdate();
      await baseProperty.GetGetterUpdatingTask();
      Assert.Equal(default, property.Getter?.String);
      Assert.Equal(default, property.Getter?.Double);
      Assert.Equal(serializedNullObject, baseProperty.Getter);
    }

    /// <summary>
    ///   Testing the value conversion for the typed get/set asynchronous property.
    /// </summary>
    [Fact]
    public async Task TypedGetSetPropertyValueConversionTest()
    {
      var property = new AsyncProperty<TestObject?>(() => RemoteObject, value => RemoteObject = value,
        ToTestObjectConverter, FromTestObjectConverter);
      var baseProperty = (IAsyncProperty) property;
      var newObject = new TestObject
      {
        String = TestValue,
        Double = DoubleTestValue
      };
      var serializedTestObject = JsonSerializer.Serialize(newObject);
      var serializedNullObject = JsonSerializer.Serialize<TestObject?>(null);
      Assert.Equal(serializedNullObject, baseProperty.Getter);
      Assert.Equal(serializedNullObject, baseProperty.Setter);

      RemoteObject = null;
      property.Setter = newObject;
      await property.GetSetterProcessingTask();
      Assert.Equal(newObject.String, RemoteObject?.String);
      Assert.Equal(newObject.Double, RemoteObject?.Double);

      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Equal(RemoteObject?.String, property.Getter?.String);
      Assert.Equal(RemoteObject?.Double, property.Getter?.Double);
      Assert.Equal(serializedTestObject, baseProperty.Getter);

      RemoteObject = null;
      baseProperty.Setter = serializedTestObject;
      await baseProperty.GetSetterProcessingTask();
      Assert.Equal(newObject.String, RemoteObject?.String);
      Assert.Equal(newObject.Double, RemoteObject?.Double);

      baseProperty.RequestGetterUpdate();
      await baseProperty.GetGetterUpdatingTask();
      Assert.Equal(RemoteObject?.String, property.Getter?.String);
      Assert.Equal(RemoteObject?.Double, property.Getter?.Double);
      Assert.Equal(serializedTestObject, baseProperty.Getter);
    }

    /// <summary>
    ///   Testing the invalid string value to type conversion.
    /// </summary>
    [Fact]
    public void InvalidStringToTypeConversionTest()
    {
      var convertedValue = AsyncProperty<double>.DefaultStringToTypeConverter("Invalid value");
      Assert.Equal(default, convertedValue);
    }
  }
}
