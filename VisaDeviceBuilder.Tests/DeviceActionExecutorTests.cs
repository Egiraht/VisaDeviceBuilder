using System;
using System.Collections.Generic;
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
    private const int DeviceActionDelay = 10;

    /// <summary>
    ///   Defines the test string value.
    /// </summary>
    private const string TestString = "Test string";

    /// <summary>
    ///   Testing device actions execution.
    /// </summary>
    [Fact]
    public async Task DeviceActionsTest()
    {
      // Synchronous device action.
      var value = string.Empty;
      Action deviceAction = () =>
      {
        Task.Delay(DeviceActionDelay).Wait();
        value = TestString;
      };
      Assert.True(DeviceActionExecutor.NoDeviceActionsAreRunning);
      Assert.True(DeviceActionExecutor.CanExecute(deviceAction));
      Assert.Empty(value);
      DeviceActionExecutor.Execute(deviceAction);
      DeviceActionExecutor.Execute(deviceAction);
      Assert.False(DeviceActionExecutor.NoDeviceActionsAreRunning);
      Assert.False(DeviceActionExecutor.CanExecute(deviceAction));
      Assert.Empty(value);
      await DeviceActionExecutor.GetDeviceActionTask(deviceAction);
      Assert.True(DeviceActionExecutor.NoDeviceActionsAreRunning);
      Assert.True(DeviceActionExecutor.CanExecute(deviceAction));
      Assert.Equal(TestString, value);

      // Asynchronous device action.
      value = string.Empty;
      Func<Task> asyncDeviceAction = async () =>
      {
        Assert.False(DeviceActionExecutor.NoDeviceActionsAreRunning);
        await Task.Delay(DeviceActionDelay);
        value = TestString;
      };
      Assert.True(DeviceActionExecutor.NoDeviceActionsAreRunning);
      Assert.True(DeviceActionExecutor.CanExecute(asyncDeviceAction));
      Assert.Empty(value);
      DeviceActionExecutor.Execute(asyncDeviceAction);
      DeviceActionExecutor.Execute(asyncDeviceAction);
      Assert.False(DeviceActionExecutor.NoDeviceActionsAreRunning);
      Assert.False(DeviceActionExecutor.CanExecute(asyncDeviceAction));
      Assert.Empty(value);
      await DeviceActionExecutor.GetDeviceActionTask(asyncDeviceAction);
      Assert.True(DeviceActionExecutor.NoDeviceActionsAreRunning);
      Assert.True(DeviceActionExecutor.CanExecute(asyncDeviceAction));
      Assert.Equal(TestString, value);
    }

    /// <summary>
    ///   Testing handling of device action exceptions.
    /// </summary>
    [Fact]
    public async Task ExceptionsTest()
    {
      var exceptionMessages = new List<string>();
      Action deviceActionWithException = () =>
      {
        Task.Delay(DeviceActionDelay).Wait();
        throw new Exception(TestString);
      };
      Func<Task> asyncDeviceActionWithException = async () =>
      {
        await Task.Delay(DeviceActionDelay);
        throw new Exception(TestString);
      };
      ThreadExceptionEventHandler exceptionHandler = (sender, args) => exceptionMessages.Add(args.Exception.Message);

      try
      {
        DeviceActionExecutor.Exception += exceptionHandler;
        DeviceActionExecutor.Execute(deviceActionWithException);
        DeviceActionExecutor.Execute(asyncDeviceActionWithException);
        Assert.Empty(exceptionMessages);

        await DeviceActionExecutor.WaitForAllActionsToCompleteAsync();
        Assert.True(DeviceActionExecutor.NoDeviceActionsAreRunning);
        Assert.Equal(2, exceptionMessages.Count);
        Assert.Equal(TestString, exceptionMessages[0]);
        Assert.Equal(TestString, exceptionMessages[1]);
      }
      finally
      {
        DeviceActionExecutor.Exception -= exceptionHandler;
      }
    }
  }
}
