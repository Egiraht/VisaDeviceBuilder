// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;
using VisaDeviceBuilder.Tests.Components;
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
    ///   Gets the test VISA device instance.
    /// </summary>
    private IVisaDevice TestVisaDevice { get; } = new TestMessageDevice();

    /// <summary>
    ///   Gets the dictionary that holds independent string values for the test device action delegate depending on the
    ///   target device.
    /// </summary>
    private Dictionary<IVisaDevice, string> TestValues { get; } = new();

    /// <summary>
    ///   Defines the test device action delegate.
    /// </summary>
    private void TestDeviceActionDelegate(IVisaDevice? visaDevice)
    {
      Task.Delay(DeviceActionDelay).Wait();
      TestValues[visaDevice!] = TestString;
    }

    /// <summary>
    ///   Testing device action execution.
    /// </summary>
    [Fact]
    public async Task DeviceActionExecutionTest()
    {
      var deviceAction = new DeviceAction(TestDeviceActionDelegate)
      {
        Name = TestName,
        TargetDevice = TestVisaDevice
      };
      Assert.Equal(TestName, deviceAction.Name);
      Assert.Same(TestVisaDevice, deviceAction.TargetDevice);
      Assert.Equal(TestDeviceActionDelegate, deviceAction.DeviceActionDelegate);
      Assert.True(deviceAction.CanExecute);
      Assert.Empty(TestValues);

      var executionTask = deviceAction.ExecuteAsync();
      _ = deviceAction.ExecuteAsync(); // Repeated call should pass OK.
      Assert.False(deviceAction.CanExecute);
      Assert.Empty(TestValues);

      await executionTask;
      Assert.True(deviceAction.CanExecute);
      Assert.Equal(TestString, TestValues[TestVisaDevice]);
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
      var deviceAction = new DeviceAction(_ =>
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
      var deviceAction = new DeviceAction(TestDeviceActionDelegate)
      {
        Name = TestName,
        TargetDevice = TestVisaDevice
      };
      var clone = (DeviceAction) deviceAction.Clone();
      Assert.Equal(TestName, clone.Name);
      Assert.Same(TestVisaDevice, clone.TargetDevice);
      Assert.Equal(TestDeviceActionDelegate, clone.DeviceActionDelegate);
      Assert.True(clone.CanExecute);
      Assert.True(deviceAction.CanExecute);

      // The cloned device action must behave as the original one.
      var executionTask = clone.ExecuteAsync();
      _ = clone.ExecuteAsync(); // Repeated call should pass OK.
      Assert.False(clone.CanExecute);
      Assert.True(deviceAction.CanExecute); // The original device action must remain executable.
      Assert.Empty(TestValues);

      await executionTask;
      Assert.True(clone.CanExecute);
      Assert.True(deviceAction.CanExecute);
      Assert.Equal(TestString, TestValues[TestVisaDevice]);
    }
  }
}
