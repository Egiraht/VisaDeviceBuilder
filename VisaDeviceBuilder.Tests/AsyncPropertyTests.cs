using System;
using System.Globalization;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  // The extension with additional unit tests covering the typed AsyncProperty class variant.
  public class AsyncPropertyTests
  {
    /// <summary>
    ///   Defines the delay in milliseconds for imitation of time-consuming asynchronous operations.
    ///   Must be greater than zero.
    /// </summary>
    private const int OperationDelay = 50;

    /// <summary>
    ///   Defines the custom double value for typed getter/setter value testing.
    /// </summary>
    private const double TestValue = Math.PI;

    /// <summary>
    ///   Defines the custom text message for getter exception testing.
    /// </summary>
    private const string GetterExceptionMessage = "Getter exception";

    /// <summary>
    ///   Defines the custom text message for setter exception testing.
    /// </summary>
    private const string SetterExceptionMessage = "Setter exception";

    /// <summary>
    ///   Gets or sets the double test value imitating a value type device parameter.
    /// </summary>
    private double PropertyValue { get; set; }

    /// <summary>
    ///   The getter method of double type that imitates time-consuming reading of the remote device parameter.
    /// </summary>
    private double GetterCallback()
    {
      Task.Delay(OperationDelay).Wait();
      return PropertyValue;
    }

    /// <summary>
    ///   The setter method of double type that imitates time-consuming writing of the remote device parameter.
    /// </summary>
    private void SetterCallback(double value)
    {
      Task.Delay(OperationDelay).Wait();
      PropertyValue = value;
    }

    /// <summary>
    ///   Testing the get-only asynchronous property.
    /// </summary>
    [Fact]
    public async Task GetOnlyPropertyTest()
    {
      var property = new AsyncProperty<double>(GetterCallback) {AutoUpdateGetterAfterSetterCompletes = false};
      var baseProperty = (IAsyncProperty) property;
      Assert.True(property.CanGet);
      Assert.False(property.CanSet);
      Assert.Equal(default, property.Getter);
      Assert.Equal(typeof(double), baseProperty.ValueType);

      // The getter must return the correct value.
      PropertyValue = TestValue;
      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Equal(TestValue, property.Getter);
      Assert.Equal(TestValue, baseProperty.Getter);

      // The setter must not change the value.
      PropertyValue = default;
      property.Setter = TestValue;
      await property.GetSetterProcessingTask();
      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Equal(default, property.Getter);
      Assert.Equal(default(double), baseProperty.Getter);
    }

    /// <summary>
    ///   Testing the set-only asynchronous property.
    /// </summary>
    [Fact]
    public async Task SetOnlyPropertyTest()
    {
      var property = new AsyncProperty<double>(SetterCallback) {AutoUpdateGetterAfterSetterCompletes = false};
      var baseProperty = (IAsyncProperty) property;
      Assert.False(property.CanGet);
      Assert.True(property.CanSet);
      Assert.Equal(default, property.Getter);
      Assert.Equal(typeof(double), baseProperty.ValueType);

      // The getter must return a default value.
      PropertyValue = TestValue;
      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Equal(default, property.Getter);
      Assert.Equal(default(double), baseProperty.Getter);

      // The setter must change the value.
      PropertyValue = default;
      property.Setter = TestValue;
      await property.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);

      PropertyValue = default;
      baseProperty.Setter = TestValue;
      await baseProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);
    }

    /// <summary>
    ///   Testing the get/set asynchronous property.
    /// </summary>
    [Fact]
    public async Task GetSetPropertyTest()
    {
      var property = new AsyncProperty<double>(GetterCallback, SetterCallback)
        {AutoUpdateGetterAfterSetterCompletes = false};
      var baseProperty = (IAsyncProperty) property;
      Assert.True(property.CanGet);
      Assert.True(property.CanSet);
      Assert.Equal(default, property.Getter);
      Assert.Equal(typeof(double), baseProperty.ValueType);

      // The getter must return the correct value.
      PropertyValue = TestValue;
      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Equal(TestValue, property.Getter);
      Assert.Equal(TestValue, baseProperty.Getter);

      // The setter must change the value.
      PropertyValue = default;
      property.Setter = TestValue;
      await property.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);

      PropertyValue = default;
      baseProperty.Setter = TestValue;
      await baseProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);

      // Testing base setter type conversion.
      baseProperty.Setter = TestValue.ToString(CultureInfo.CurrentCulture);
      await baseProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);
    }

    /// <summary>
    ///   Testing base setter type conversion.
    /// </summary>
    [Fact]
    public async Task BaseSetterTypeConversionTest()
    {
      IAsyncProperty baseProperty = new AsyncProperty<double>(GetterCallback, SetterCallback)
        {AutoUpdateGetterAfterSetterCompletes = false};

      // String to double conversion.
      PropertyValue = default;
      baseProperty.Setter = TestValue.ToString(CultureInfo.CurrentCulture);
      await baseProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);

      // Double to double conversion.
      PropertyValue = default;
      baseProperty.Setter = TestValue;
      await baseProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);

      // Incompatible type conversion.
      PropertyValue = default;
      baseProperty.Setter = new object();
      await baseProperty.GetSetterProcessingTask();
      Assert.Equal(default, PropertyValue);

      // Null value conversion.
      PropertyValue = default;
      baseProperty.Setter = null;
      await baseProperty.GetSetterProcessingTask();
      Assert.Equal(default, PropertyValue);
    }

    /// <summary>
    ///   Testing auto-updating of the getter after setter value processing completes.
    /// </summary>
    [Fact]
    public async Task GetterAutoUpdatingTest()
    {
      var property = new AsyncProperty<double>(GetterCallback, SetterCallback)
        {AutoUpdateGetterAfterSetterCompletes = false};
      Assert.Equal(default, property.Getter);

      // Getter auto-update is disabled, the getter value must not get updated automatically.
      PropertyValue = default;
      property.Setter = TestValue;
      await property.GetSetterProcessingTask();
      await property.GetGetterUpdatingTask();
      Assert.Equal(default, property.Getter);

      // Getter auto-update is enabled, the getter value must get updated automatically.
      PropertyValue = default;
      property.AutoUpdateGetterAfterSetterCompletes = true;
      property.Setter = TestValue;
      await property.GetSetterProcessingTask();
      await property.GetGetterUpdatingTask();
      Assert.Equal(TestValue, property.Getter);
    }

    /// <summary>
    ///   Testing the getter/setter events in the asynchronous property.
    /// </summary>
    [Fact]
    public async Task PropertyEventsTest()
    {
      var getterPassed = false;
      var setterPassed = false;
      var property = new AsyncProperty<double>(GetterCallback, SetterCallback)
        {AutoUpdateGetterAfterSetterCompletes = false};
      property.GetterUpdated += (_, _) => getterPassed = true;
      property.SetterCompleted += (_, _) => setterPassed = true;

      // Updating getter, getter updated event must be called, setter completed event must not.
      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.True(getterPassed);
      Assert.False(setterPassed);

      // Changing setter, setter completed event must be called, getter updated event must not.
      getterPassed = false;
      property.Setter = TestValue;
      await property.GetSetterProcessingTask();
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
      var property = new AsyncProperty<double>(
          () => throw new Exception(GetterExceptionMessage),
          _ => throw new Exception(SetterExceptionMessage))
        {AutoUpdateGetterAfterSetterCompletes = false};
      property.GetterException += (_, e) => getterException = e.Exception;
      property.SetterException += (_, e) => setterException = e.Exception;
      Assert.Null(getterException);
      Assert.Null(setterException);

      // Updating getter, getter exception event must be called, setter exception event must not.
      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Null(setterException);
      Assert.Equal(GetterExceptionMessage, getterException?.Message);

      // Changing setter, setter exception event must be called, getter exception event must not.
      getterException = null;
      property.Setter = TestValue;
      await property.GetSetterProcessingTask();
      Assert.Null(getterException);
      Assert.Equal(SetterExceptionMessage, setterException?.Message);
    }
  }
}
