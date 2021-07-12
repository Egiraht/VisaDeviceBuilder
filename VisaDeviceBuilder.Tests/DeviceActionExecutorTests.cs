using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="DeviceActionExecutor" /> class.
  /// </summary>
  public class DeviceActionExecutorTests
  {
    /// <summary>
    ///   Defines the asynchronous device action delay to simulate long operations.
    /// </summary>
    private const int DeviceActionDelay = 1;

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
      var deviceAction = new DeviceAction(TestDeviceActionDelegate);
      Assert.True(DeviceActionExecutor.NoDeviceActionsAreRunning);
      Assert.True(DeviceActionExecutor.CanExecute(deviceAction));
      Assert.Empty(TestValue);

      DeviceActionExecutor.BeginExecute(deviceAction);
      DeviceActionExecutor.BeginExecute(deviceAction); // Repeated call should pass OK.
      Assert.False(DeviceActionExecutor.NoDeviceActionsAreRunning);
      Assert.False(DeviceActionExecutor.CanExecute(deviceAction));
      Assert.Empty(TestValue);

      await DeviceActionExecutor.GetDeviceActionTask(deviceAction);
      Assert.True(DeviceActionExecutor.NoDeviceActionsAreRunning);
      Assert.True(DeviceActionExecutor.CanExecute(deviceAction));
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

      void CompletionHandler(object? sender, EventArgs args) => completed = true;

      try
      {
        DeviceActionExecutor.DeviceActionCompleted += CompletionHandler;
        DeviceActionExecutor.BeginExecute(deviceAction);
        Assert.False(DeviceActionExecutor.CanExecute(deviceAction));
        Assert.False(completed);

        await DeviceActionExecutor.GetDeviceActionTask(deviceAction);
        Assert.True(DeviceActionExecutor.CanExecute(deviceAction));
        Assert.True(completed);
      }
      finally
      {
        DeviceActionExecutor.DeviceActionCompleted -= CompletionHandler;
      }
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

      void ExceptionHandler(object sender, ThreadExceptionEventArgs args)
      {
        source = sender;
        exception = args.Exception;
      }

      try
      {
        DeviceActionExecutor.Exception += ExceptionHandler;
        DeviceActionExecutor.BeginExecute(deviceAction);
        Assert.False(DeviceActionExecutor.CanExecute(deviceAction));
        Assert.Null(source);
        Assert.Null(exception);

        await DeviceActionExecutor.WaitForAllActionsToCompleteAsync();
        Assert.True(DeviceActionExecutor.NoDeviceActionsAreRunning);
        Assert.True(DeviceActionExecutor.CanExecute(deviceAction));
        Assert.Same(deviceAction, source);
        Assert.Equal(TestString, exception!.Message);
      }
      finally
      {
        DeviceActionExecutor.Exception -= ExceptionHandler;
      }
    }
  }
}
