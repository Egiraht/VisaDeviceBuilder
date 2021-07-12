using System;
using System.Globalization;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;
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
      var asyncProperty = new AsyncProperty<double>(GetterCallback)
      {
        Name = TestName,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var baseAsyncProperty = (IAsyncProperty) asyncProperty;
      Assert.True(asyncProperty.CanGet);
      Assert.False(asyncProperty.CanSet);
      Assert.Equal(GetterCallback, asyncProperty.GetterDelegate);
      asyncProperty.SetterDelegate.Invoke(default); // The default setter delegate should pass OK.
      Assert.Equal(default, asyncProperty.Getter);
      Assert.Equal(typeof(double), baseAsyncProperty.ValueType);
      Assert.Equal(TestName, asyncProperty.Name);
      Assert.False(asyncProperty.AutoUpdateGetterAfterSetterCompletes);

      // The getter must return the correct value.
      PropertyValue = TestValue;
      asyncProperty.RequestGetterUpdate();
      await asyncProperty.GetGetterUpdatingTask();
      Assert.Equal(TestValue, asyncProperty.Getter);
      Assert.Equal(TestValue, baseAsyncProperty.Getter);

      // The setter must not change the value.
      PropertyValue = default;
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
    public async Task SetOnlyPropertyTest()
    {
      var asyncProperty = new AsyncProperty<double>(SetterCallback)
      {
        Name = TestName,
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var baseAsyncProperty = (IAsyncProperty) asyncProperty;
      Assert.False(asyncProperty.CanGet);
      Assert.True(asyncProperty.CanSet);
      Assert.Equal(default, asyncProperty.GetterDelegate.Invoke()); // The default getter delegate should pass OK.
      Assert.Equal(SetterCallback, asyncProperty.SetterDelegate);
      Assert.Equal(default, asyncProperty.Getter);
      Assert.Equal(typeof(double), baseAsyncProperty.ValueType);
      Assert.Equal(TestName, asyncProperty.Name);
      Assert.False(asyncProperty.AutoUpdateGetterAfterSetterCompletes);

      // The getter must return a default value.
      PropertyValue = TestValue;
      asyncProperty.RequestGetterUpdate();
      await asyncProperty.GetGetterUpdatingTask();
      Assert.Equal(default, asyncProperty.Getter);
      Assert.Equal(default(double), baseAsyncProperty.Getter);

      // The setter must change the value.
      PropertyValue = default;
      asyncProperty.Setter = TestValue;
      await asyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);

      PropertyValue = default;
      baseAsyncProperty.Setter = TestValue;
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);
    }

    /// <summary>
    ///   Testing the get/set asynchronous property.
    /// </summary>
    [Fact]
    public async Task GetSetPropertyTest()
    {
      var asyncProperty = new AsyncProperty<double>(GetterCallback, SetterCallback)
      {
        Name = TestName,
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
      Assert.False(asyncProperty.AutoUpdateGetterAfterSetterCompletes);

      // The getter must return the correct value.
      PropertyValue = TestValue;
      asyncProperty.RequestGetterUpdate();
      await asyncProperty.GetGetterUpdatingTask();
      Assert.Equal(TestValue, asyncProperty.Getter);
      Assert.Equal(TestValue, baseAsyncProperty.Getter);

      // The setter must change the value.
      PropertyValue = default;
      asyncProperty.Setter = TestValue;
      await asyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);

      PropertyValue = default;
      baseAsyncProperty.Setter = TestValue;
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);

      // Testing base setter type conversion.
      baseAsyncProperty.Setter = TestValue.ToString(CultureInfo.CurrentCulture);
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);
    }

    /// <summary>
    ///   Testing base setter type conversion.
    /// </summary>
    [Fact]
    public async Task BaseSetterTypeConversionTest()
    {
      IAsyncProperty baseAsyncProperty = new AsyncProperty<double>(GetterCallback, SetterCallback)
        {AutoUpdateGetterAfterSetterCompletes = false};

      // String to double conversion.
      PropertyValue = default;
      baseAsyncProperty.Setter = TestValue.ToString(CultureInfo.CurrentCulture);
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);

      // Double to double conversion.
      PropertyValue = default;
      baseAsyncProperty.Setter = TestValue;
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);

      // Incompatible type conversion.
      PropertyValue = default;
      baseAsyncProperty.Setter = new object();
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(default, PropertyValue);

      // Null value conversion.
      PropertyValue = default;
      baseAsyncProperty.Setter = null;
      await baseAsyncProperty.GetSetterProcessingTask();
      Assert.Equal(default, PropertyValue);
    }

    /// <summary>
    ///   Testing auto-updating of the getter after setter value processing completes.
    /// </summary>
    [Fact]
    public async Task GetterAutoUpdatingTest()
    {
      var asyncProperty = new AsyncProperty<double>(GetterCallback, SetterCallback)
        {AutoUpdateGetterAfterSetterCompletes = false};
      Assert.Equal(default, asyncProperty.Getter);

      // Getter auto-update is disabled, the getter value must not get updated automatically.
      PropertyValue = default;
      asyncProperty.Setter = TestValue;
      await asyncProperty.GetSetterProcessingTask();
      await asyncProperty.GetGetterUpdatingTask();
      Assert.Equal(default, asyncProperty.Getter);

      // Getter auto-update is enabled, the getter value must get updated automatically.
      PropertyValue = default;
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
        {AutoUpdateGetterAfterSetterCompletes = false};
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
          () => throw new Exception(GetterExceptionMessage),
          _ => throw new Exception(SetterExceptionMessage))
        {AutoUpdateGetterAfterSetterCompletes = false};
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
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var clone = (AsyncProperty<double>) asyncProperty.Clone();
      Assert.True(clone.CanGet);
      Assert.False(clone.CanSet);
      Assert.Equal(GetterCallback, clone.GetterDelegate);
      Assert.Equal(default, clone.Getter);
      Assert.Equal(typeof(double), clone.ValueType);
      Assert.Equal(TestName, clone.Name);
      Assert.False(clone.AutoUpdateGetterAfterSetterCompletes);

      // The clone's getter must return the correct value.
      PropertyValue = TestValue;
      clone.RequestGetterUpdate();
      await clone.GetGetterUpdatingTask();
      Assert.Equal(TestValue, clone.Getter);

      // The clone's setter must not change the value.
      PropertyValue = default;
      clone.Setter = TestValue;
      await clone.GetSetterProcessingTask();
      Assert.Equal(default, PropertyValue);
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
        AutoUpdateGetterAfterSetterCompletes = false
      };
      var clone = (AsyncProperty<double>) asyncProperty.Clone();
      Assert.False(clone.CanGet);
      Assert.True(clone.CanSet);
      Assert.Equal(SetterCallback, clone.SetterDelegate);
      Assert.Equal(default, clone.Getter);
      Assert.Equal(typeof(double), clone.ValueType);
      Assert.Equal(TestName, clone.Name);
      Assert.False(clone.AutoUpdateGetterAfterSetterCompletes);

      // The clone's getter must return a default value.
      PropertyValue = TestValue;
      clone.RequestGetterUpdate();
      await clone.GetGetterUpdatingTask();
      Assert.Equal(default, clone.Getter);

      // The clone's setter must change the value.
      PropertyValue = default;
      clone.Setter = TestValue;
      await clone.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);
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
      Assert.False(clone.AutoUpdateGetterAfterSetterCompletes);

      // The clone's getter must return the correct value.
      PropertyValue = TestValue;
      clone.RequestGetterUpdate();
      await clone.GetGetterUpdatingTask();
      Assert.Equal(TestValue, clone.Getter);

      // The clone's setter must change the value.
      PropertyValue = default;
      clone.Setter = TestValue;
      await clone.GetSetterProcessingTask();
      Assert.Equal(TestValue, PropertyValue);
    }
  }
}
