using System;
using System.Threading.Tasks;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="AsyncProperty" /> class.
  /// </summary>
  public partial class AsyncPropertyTests
  {
    /// <summary>
    ///   Defines the delay in milliseconds for imitation of time-consuming asynchronous operations.
    ///   Must be greater than zero.
    /// </summary>
    private const int OperationDelay = 50;

    /// <summary>
    ///   Defines the custom text string for getter/setter value testing.
    /// </summary>
    private const string TestValue = "Test text";

    /// <summary>
    ///   Defines the custom text message for getter exception testing.
    /// </summary>
    private const string GetterExceptionMessage = "Getter exception";

    /// <summary>
    ///   Defines the custom text message for setter exception testing.
    /// </summary>
    private const string SetterExceptionMessage = "Setter exception";

    /// <summary>
    ///   Gets or sets the test value imitating a remote device parameter value.
    /// </summary>
    private string RemoteValue { get; set; } = string.Empty;

    /// <summary>
    ///   The getter method that imitates time-consuming reading of the remote device parameter.
    /// </summary>
    private string GetterCallback()
    {
      Task.Delay(OperationDelay).Wait();
      return RemoteValue;
    }

    /// <summary>
    ///   The setter method that imitates time-consuming writing of the remote device parameter.
    /// </summary>
    private void SetterCallback(string value)
    {
      Task.Delay(OperationDelay).Wait();
      RemoteValue = value;
    }

    /// <summary>
    ///   Testing the get-only asynchronous property.
    /// </summary>
    [Fact]
    public async Task GetOnlyPropertyTest()
    {
      var property = new AsyncProperty(GetterCallback);
      Assert.True(property.CanGet);
      Assert.False(property.CanSet);
      Assert.Empty(property.Getter);
      Assert.Empty(property.Setter);

      RemoteValue = TestValue;
      property.Setter = string.Empty;
      await property.GetSetterProcessingTask();
      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Equal(RemoteValue, property.Getter);
      Assert.Empty(property.Setter);
    }

    /// <summary>
    ///   Testing the set-only asynchronous property.
    /// </summary>
    [Fact]
    public async Task SetOnlyPropertyTest()
    {
      var property = new AsyncProperty(SetterCallback);
      Assert.False(property.CanGet);
      Assert.True(property.CanSet);
      Assert.Empty(property.Getter);
      Assert.Empty(property.Setter);

      RemoteValue = string.Empty;
      property.Setter = TestValue;
      Assert.Equal(TestValue, property.Setter);
      Assert.Empty(RemoteValue);

      await property.GetSetterProcessingTask();
      Assert.Empty(property.Setter);
      Assert.Equal(TestValue, RemoteValue);

      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Empty(property.Getter);
    }

    /// <summary>
    ///   Testing the get/set asynchronous property.
    /// </summary>
    [Fact]
    public async Task GetSetPropertyTest()
    {
      var property = new AsyncProperty(GetterCallback, SetterCallback);
      Assert.True(property.CanGet);
      Assert.True(property.CanSet);
      Assert.Empty(property.Getter);
      Assert.Empty(property.Setter);

      RemoteValue = string.Empty;
      property.Setter = TestValue;
      Assert.Equal(TestValue, property.Setter);
      Assert.Empty(RemoteValue);

      await property.GetSetterProcessingTask();
      Assert.Empty(property.Setter);
      Assert.Equal(TestValue, RemoteValue);

      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Equal(TestValue, property.Getter);

      RemoteValue = string.Empty;
      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Empty(property.Getter);
    }

    /// <summary>
    ///   Testing auto-updating of the getter after setter value processing completes.
    /// </summary>
    [Fact]
    public async Task GetterAutoUpdatingTest()
    {
      var property = new AsyncProperty(GetterCallback, SetterCallback);
      property.AutoUpdateGetterAfterSetterCompletes = false;
      Assert.Empty(property.Getter);

      property.Setter = TestValue;
      await property.GetSetterProcessingTask();
      Assert.Empty(property.Getter);

      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Equal(TestValue, property.Getter);

      property.AutoUpdateGetterAfterSetterCompletes = true;
      property.Setter = string.Empty;
      Assert.Equal(TestValue, property.Getter);

      await property.GetSetterProcessingTask();
      Assert.Empty(property.Getter);
    }

    /// <summary>
    ///   Testing the getter/setter events in the asynchronous property.
    /// </summary>
    [Fact]
    public async Task PropertyEventsTest()
    {
      var getterPassed = false;
      var setterPassed = false;
      var lastChangedPropertyName = string.Empty;
      var property = new AsyncProperty(GetterCallback, SetterCallback);
      property.AutoUpdateGetterAfterSetterCompletes = false;
      property.GetterUpdated += (_, e) => getterPassed = true;
      property.SetterCompleted += (_, e) => setterPassed = true;
      property.PropertyChanged += (_, e) => lastChangedPropertyName = e.PropertyName;

      property.Setter = TestValue;
      Assert.False(getterPassed);
      Assert.False(setterPassed);
      Assert.Empty(lastChangedPropertyName);

      await property.GetSetterProcessingTask();
      Assert.False(getterPassed);
      Assert.True(setterPassed);
      Assert.Equal(nameof(AsyncProperty.Setter), lastChangedPropertyName);

      setterPassed = false;
      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.True(getterPassed);
      Assert.False(setterPassed);
      Assert.Equal(nameof(AsyncProperty.Getter), lastChangedPropertyName);
    }

    /// <summary>
    ///   Testing the exception events in the asynchronous property.
    /// </summary>
    [Fact]
    public async Task PropertyExceptionsTest()
    {
      Exception? getterException = null;
      Exception? setterException = null;
      var property = new AsyncProperty(() => throw new Exception(GetterExceptionMessage),
        _ => throw new Exception(SetterExceptionMessage));
      property.AutoUpdateGetterAfterSetterCompletes = false;
      property.GetterException += (_, e) => getterException = e.Exception;
      property.SetterException += (_, e) => setterException = e.Exception;
      Assert.Null(getterException);
      Assert.Null(setterException);

      property.Setter = TestValue;
      await property.GetSetterProcessingTask();
      Assert.Null(getterException);
      Assert.Equal(SetterExceptionMessage, setterException?.Message);

      setterException = null;
      property.RequestGetterUpdate();
      await property.GetGetterUpdatingTask();
      Assert.Null(setterException);
      Assert.Equal(GetterExceptionMessage, getterException?.Message);
    }
  }
}
