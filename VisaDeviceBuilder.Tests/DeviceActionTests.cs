using System;
using System.Threading.Tasks;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="DeviceAction" /> class.
  /// </summary>
  public class DeviceActionTests
  {
    /// <summary>
    ///   Defines the asynchronous device action delay to simulate long operations.
    /// </summary>
    private const int DeviceActionDelay = 1;

    /// <summary>
    ///   Defines the test device action name.
    /// </summary>
    private const string TestName = "Test name";

    /// <summary>
    ///   Defines the test string value.
    /// </summary>
    private const string TestString = "Test string";

    /// <summary>
    ///   Gets or sets the value of the test device action.
    /// </summary>
    private string TestValue { get; set; } = string.Empty;

    /// <summary>
    ///   Defines the test device action delegate.
    /// </summary>
    private void TestDeviceActionDelegate()
    {
      Task.Delay(DeviceActionDelay).Wait();
      TestValue = TestString;
    }

    /// <summary>
    ///   Testing device action execution.
    /// </summary>
    [Fact]
    public async Task DeviceActionExecutionTest()
    {
      var deviceAction = new DeviceAction(TestDeviceActionDelegate) {Name = TestName};
      Assert.Equal(TestName, deviceAction.Name);
      Assert.Equal(TestDeviceActionDelegate, deviceAction.DeviceActionDelegate);
      Assert.True(deviceAction.CanExecute);
      Assert.Empty(TestValue);

      var executionTask = deviceAction.ExecuteAsync();
      _ = deviceAction.ExecuteAsync(); // Repeated call should pass OK.
      Assert.False(deviceAction.CanExecute);
      Assert.Empty(TestValue);

      await executionTask;
      Assert.True(deviceAction.CanExecute);
      Assert.Equal(TestString, TestValue);
    }

    /// <summary>
    ///   Testing device action completion event handling.
    /// </summary>
    [Fact]
    public async Task DeviceActionCompletionTest()
    {
      var completed = false;
      var deviceAction = new DeviceAction(TestDeviceActionDelegate);
      deviceAction.ExecutionCompleted += (_, _) => completed = true;

      var executionTask = deviceAction.ExecuteAsync();
      Assert.False(deviceAction.CanExecute);
      Assert.False(completed);

      await executionTask;
      Assert.True(deviceAction.CanExecute);
      Assert.True(completed);
    }

    /// <summary>
    ///   Testing device action exception event handling.
    /// </summary>
    [Fact]
    public async Task DeviceActionExceptionTest()
    {
      object? source = null;
      Exception? exception = null;
      var deviceAction = new DeviceAction(() =>
      {
        Task.Delay(DeviceActionDelay).Wait();
        throw new Exception(TestString);
      });
      deviceAction.Exception += (sender, args) =>
      {
        source = sender;
        exception = args.Exception;
      };

      var executionTask = deviceAction.ExecuteAsync();
      Assert.False(deviceAction.CanExecute);
      Assert.Null(source);
      Assert.Null(exception);

      await executionTask;
      Assert.True(deviceAction.CanExecute);
      Assert.Same(deviceAction, source);
      Assert.Equal(TestString, exception!.Message);
    }

    /// <summary>
    ///   Testing device action cloning.
    /// </summary>
    [Fact]
    public async Task DeviceActionCloningTest()
    {
      var deviceAction = new DeviceAction(TestDeviceActionDelegate) {Name = TestName};
      var clone = (DeviceAction) deviceAction.Clone();
      Assert.Equal(TestName, clone.Name);
      Assert.Equal(TestDeviceActionDelegate, clone.DeviceActionDelegate);
      Assert.True(clone.CanExecute);
      Assert.True(deviceAction.CanExecute);

      // The cloned device action must behave as the original one.
      var executionTask = clone.ExecuteAsync();
      _ = clone.ExecuteAsync(); // Repeated call should pass OK.
      Assert.False(clone.CanExecute);
      Assert.True(deviceAction.CanExecute); // The original device action must remain executable.
      Assert.Empty(TestValue);

      await executionTask;
      Assert.True(clone.CanExecute);
      Assert.True(deviceAction.CanExecute);
      Assert.Equal(TestString, TestValue);
    }
  }
}
