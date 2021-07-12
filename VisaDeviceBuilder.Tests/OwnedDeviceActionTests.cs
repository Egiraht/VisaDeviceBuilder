using System;
using System.Threading.Tasks;
using VisaDeviceBuilder.Tests.Components;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="OwnedDeviceAction{TOwner}" /> class.
  /// </summary>
  public class OwnedDeviceActionTests
  {
    /// <summary>
    ///   Defines the test device action name.
    /// </summary>
    private const string TestName = "Test name";

    /// <summary>
    ///   Gets the test message-based VISA device instance.
    /// </summary>
    private TestMessageDevice Device { get; } = new();

    /// <summary>
    ///   Defines the test owned device action delegate.
    ///   Executes the owning device's <see cref="TestMessageDevice.DeclaredTestDeviceAction" /> device action.
    /// </summary>
    private void TestOwnedDeviceActionDelegate(TestMessageDevice device) =>
      device.DeclaredTestDeviceAction.ExecuteAsync().Wait();

    /// <summary>
    ///   Testing device action execution.
    /// </summary>
    [Fact]
    public async Task DeviceActionExecutionTest()
    {
      var ownedDeviceAction = new OwnedDeviceAction<TestMessageDevice>(TestOwnedDeviceActionDelegate)
      {
        Owner = Device,
        Name = TestName
      };
      Assert.Same(Device, ownedDeviceAction.Owner);
      Assert.Equal(TestName, ownedDeviceAction.Name);
      Assert.Equal(TestOwnedDeviceActionDelegate, ownedDeviceAction.OwnedDeviceActionDelegate);
      Assert.True(ownedDeviceAction.CanExecute);
      Assert.False(Device.IsDeclaredTestDeviceActionCalled);

      var executionTask = ownedDeviceAction.ExecuteAsync();
      _ = ownedDeviceAction.ExecuteAsync(); // Repeated call should pass OK.
      Assert.False(ownedDeviceAction.CanExecute);
      Assert.False(Device.IsDeclaredTestDeviceActionCalled);

      await executionTask;
      Assert.True(ownedDeviceAction.CanExecute);
      Assert.True(Device.IsDeclaredTestDeviceActionCalled);
    }

    /// <summary>
    ///   Testing ownership change of an owned asynchronous property.
    /// </summary>
    [Fact]
    public async Task OwnershipChangeTest()
    {
      var device1 = new TestMessageDevice();
      var device2 = new TestMessageDevice();
      var ownedDeviceAction = new OwnedDeviceAction<TestMessageDevice>(TestOwnedDeviceActionDelegate) {Owner = device1};
      Assert.Equal(device1, ownedDeviceAction.Owner);
      Assert.False(device1.IsDeclaredTestDeviceActionCalled);
      Assert.False(device2.IsDeclaredTestDeviceActionCalled);

      // Testing ownership of the device1, this must not influence the device2.
      await ownedDeviceAction.ExecuteAsync();
      Assert.Equal(device1, ownedDeviceAction.Owner);
      Assert.True(device1.IsDeclaredTestDeviceActionCalled);
      Assert.False(device2.IsDeclaredTestDeviceActionCalled);

      // Testing ownership of the device2, this must not influence the device1.
      device1.IsDeclaredTestDeviceActionCalled = false;
      ownedDeviceAction.Owner = device2;
      await ownedDeviceAction.ExecuteAsync();
      Assert.Equal(device2, ownedDeviceAction.Owner);
      Assert.False(device1.IsDeclaredTestDeviceActionCalled);
      Assert.True(device2.IsDeclaredTestDeviceActionCalled);
    }

    /// <summary>
    ///   Testing the exception thrown when no owning device is specified.
    /// </summary>
    [Fact]
    public async Task NoOwnerExceptionTest()
    {
      Exception? exception = null;
      var ownedDeviceAction = new OwnedDeviceAction<TestMessageDevice>(TestOwnedDeviceActionDelegate) {Owner = null};
      ownedDeviceAction.Exception += (_, e) => exception = e.Exception;
      Assert.Null(exception);

      await ownedDeviceAction.ExecuteAsync();
      Assert.IsType<InvalidOperationException>(exception);
    }

    /// <summary>
    ///   Testing device action cloning.
    /// </summary>
    [Fact]
    public async Task DeviceActionCloningTest()
    {
      var deviceAction = new OwnedDeviceAction<TestMessageDevice>(TestOwnedDeviceActionDelegate)
      {
        Owner = Device,
        Name = TestName
      };
      var clone = (OwnedDeviceAction<TestMessageDevice>) deviceAction.Clone();
      Assert.Same(Device, clone.Owner);
      Assert.Equal(TestName, clone.Name);
      Assert.Equal(TestOwnedDeviceActionDelegate, clone.OwnedDeviceActionDelegate);
      Assert.True(clone.CanExecute);
      Assert.True(deviceAction.CanExecute);
      Assert.False(Device.IsDeclaredTestDeviceActionCalled);

      // The cloned device action must behave as the original one.
      var executionTask = clone.ExecuteAsync();
      _ = clone.ExecuteAsync(); // Repeated call should pass OK.
      Assert.False(clone.CanExecute);
      Assert.True(deviceAction.CanExecute); // The original device must remain executable.
      Assert.False(Device.IsDeclaredTestDeviceActionCalled);

      await executionTask;
      Assert.True(clone.CanExecute);
      Assert.True(deviceAction.CanExecute);
      Assert.True(Device.IsDeclaredTestDeviceActionCalled);
    }
  }
}
