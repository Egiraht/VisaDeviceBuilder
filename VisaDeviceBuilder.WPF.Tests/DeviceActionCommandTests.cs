using System.Threading.Tasks;
using VisaDeviceBuilder.WPF.Components;
using Xunit;

namespace VisaDeviceBuilder.WPF.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="DeviceActionCommand" /> class.
  /// </summary>
  public class DeviceActionCommandTests
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
    ///   Testing the singleton class instance uniqueness.
    /// </summary>
    [Fact]
    public void SingletonInstanceTest()
    {
      Assert.NotNull(DeviceActionCommand.Instance);
      Assert.Same(DeviceActionCommand.Instance, DeviceActionCommand.Instance);
    }

    /// <summary>
    ///   Testing device actions execution.
    /// </summary>
    [Fact]
    public async Task DeviceActionTest()
    {
      var value = string.Empty;
      var deviceAction = new DeviceAction(() =>
      {
        Task.Delay(DeviceActionDelay).Wait();
        value = TestString;
      });
      Assert.True(DeviceActionCommand.Instance.CanExecute(deviceAction));
      Assert.Empty(value);

      DeviceActionCommand.Instance.Execute(deviceAction);
      DeviceActionCommand.Instance.Execute(deviceAction); // Repeated call should pass OK.
      Assert.False(DeviceActionCommand.Instance.CanExecute(deviceAction));

      await DeviceActionExecutor.GetDeviceActionTask(deviceAction);
      Assert.True(DeviceActionCommand.Instance.CanExecute(deviceAction));
      Assert.Equal(TestString, value);
    }

    /// <summary>
    ///   Testing an invalid parameter.
    /// </summary>
    [Fact]
    public void InvalidParameterTest()
    {
      Assert.False(DeviceActionCommand.Instance.CanExecute(null));
      DeviceActionCommand.Instance.Execute(null);
    }
  }
}
