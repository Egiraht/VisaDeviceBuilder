using System;
using System.Threading.Tasks;
using VisaDeviceBuilder.WPF.Components;
using Xunit;

namespace VisaDeviceBuilder.WPF.Tests
{
  public class DeviceActionCommandTests
  {
    private const int DeviceActionDelay = 10;

    private const string TestString = "Test string";

    [Fact]
    public void SingletonInstanceTest()
    {
      Assert.NotNull(DeviceActionCommand.Instance);
      Assert.Same(DeviceActionCommand.Instance, DeviceActionCommand.Instance);
    }

    [Fact]
    public async Task DeviceActionTest()
    {
      // Invalid device action type.
      var value = string.Empty;
      Assert.False(DeviceActionCommand.Instance.CanExecute(null));
      DeviceActionCommand.Instance.Execute(null);
      Assert.Empty(value);

      // Synchronous device action.
      Action deviceAction = () =>
      {
        Task.Delay(DeviceActionDelay).Wait();
        value = TestString;
      };
      Assert.True(DeviceActionCommand.Instance.CanExecute(deviceAction));
      Assert.Empty(value);
      DeviceActionCommand.Instance.Execute(deviceAction);
      Assert.False(DeviceActionCommand.Instance.CanExecute(deviceAction));
      Assert.Empty(value);
      await DeviceActionExecutor.GetDeviceActionTask(deviceAction);
      Assert.True(DeviceActionCommand.Instance.CanExecute(deviceAction));
      Assert.Equal(TestString, value);

      // Asynchronous device action.
      value = string.Empty;
      Func<Task> asyncDeviceAction = async () =>
      {
        Assert.False(DeviceActionExecutor.NoDeviceActionsAreRunning);
        await Task.Delay(DeviceActionDelay);
        value = TestString;
      };
      Assert.True(DeviceActionCommand.Instance.CanExecute(asyncDeviceAction));
      Assert.Empty(value);
      DeviceActionCommand.Instance.Execute(asyncDeviceAction);
      Assert.False(DeviceActionCommand.Instance.CanExecute(asyncDeviceAction));
      Assert.Empty(value);
      await DeviceActionExecutor.GetDeviceActionTask(asyncDeviceAction);
      Assert.True(DeviceActionCommand.Instance.CanExecute(asyncDeviceAction));
      Assert.Equal(TestString, value);
    }
  }
}
