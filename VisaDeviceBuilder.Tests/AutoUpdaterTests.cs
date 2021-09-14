// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;
using VisaDeviceBuilder.Tests.Components;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="AutoUpdater" /> class.
  /// </summary>
  public class AutoUpdaterTests
  {
    /// <summary>
    ///   Defines the number of auto-update loops to wait for.
    /// </summary>
    private const int LoopsCount = 3;

    /// <summary>
    ///   Defines the auto-update loop delay.
    /// </summary>
    private static readonly TimeSpan AutoUpdateDelay = TimeSpan.FromMilliseconds(1);

    /// <summary>
    ///   Defines the message for the test exception.
    /// </summary>
    private const string TestExceptionMessage = "Test exception message";

    /// <summary>
    ///   Defines the asynchronous operation timeout period.
    /// </summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    /// <summary>
    ///   Testing the auto-update loop running for the test device.
    /// </summary>
    [Fact]
    public async Task AutoUpdaterLoopTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new TestMessageDevice
      {
        ResourceManager = resourceManager,
        ResourceName = TestResourceManager.SerialTestDeviceResourceName
      };
      await using var autoUpdater = new AutoUpdater(device) { Delay = AutoUpdateDelay };
      Assert.False(autoUpdater.IsRunning);
      Assert.Equal(device.AsyncProperties, autoUpdater.AsyncProperties);
      Assert.Equal(default, device.TestAsyncProperty.Getter);

      var cycleCounter = 0;
      autoUpdater.AutoUpdateCycle += (_, _) => cycleCounter += cycleCounter < LoopsCount ? 1 : 0;
      autoUpdater.Start();
      autoUpdater.Start(); // Repeated call should pass OK.
      Assert.True(autoUpdater.IsRunning);

      var timer = Task.Delay(Timeout);
      while (cycleCounter < LoopsCount && !timer.IsCompleted) // The operation should complete before the timer.
        await Task.Delay(AutoUpdateDelay);
      Assert.Equal(LoopsCount, cycleCounter);

      await autoUpdater.StopAsync();
      await autoUpdater.StopAsync(); // Repeated call should pass OK.
      Assert.False(autoUpdater.IsRunning);
    }

    /// <summary>
    ///   Testing the auto-update loop cancellation when no asynchronous properties are available.
    /// </summary>
    [Fact]
    public void EmptyAutoUpdateLoopTest()
    {
      using var autoUpdater = new AutoUpdater(Array.Empty<IAsyncProperty>()) { Delay = AutoUpdateDelay };
      autoUpdater.Start();
      Assert.False(autoUpdater.IsRunning);
    }

    /// <summary>
    ///   Testing the auto-updater loop exception handling.
    /// </summary>
    [Fact]
    public async Task AutoUpdaterLoopExceptionTest()
    {
      // Testing exceptions caused by asynchronous properties.
      await using (var autoUpdater = new AutoUpdater(
          new[] { new AsyncProperty<string>(_ => throw new Exception(TestExceptionMessage)) })
        { Delay = AutoUpdateDelay })
      {
        Exception? exception = null;
        autoUpdater.AutoUpdateException += (_, args) => exception = args.Exception;
        autoUpdater.Start();
        Assert.True(autoUpdater.IsRunning);
        Assert.Null(exception);

        var timer = Task.Delay(Timeout);
        while (exception == null && !timer.IsCompleted) // The operation should complete before the timer.
          await Task.Delay(AutoUpdateDelay);
        Assert.Equal(TestExceptionMessage, exception?.Message);
      }

      // Testing exceptions caused by an AutoUpdateCycle event handler.
      await using (var autoUpdater = new AutoUpdater(
          new[] { new AsyncProperty<object?>(_ => null) })
        { Delay = AutoUpdateDelay })
      {
        Exception? exception = null;
        autoUpdater.AutoUpdateException += (_, args) => exception = args.Exception;
        autoUpdater.AutoUpdateCycle += (_, _) => throw new Exception(TestExceptionMessage);
        autoUpdater.Start();
        Assert.True(autoUpdater.IsRunning);
        Assert.Null(exception);

        var timer = Task.Delay(Timeout);
        while (exception == null && !timer.IsCompleted) // The operation should complete before the timer.
          await Task.Delay(AutoUpdateDelay);
        Assert.Equal(TestExceptionMessage, exception?.Message);
      }
    }

    /// <summary>
    ///   Testing the auto-updater disposal.
    /// </summary>
    [Fact]
    public async Task AutoUpdaterDisposalTest()
    {
      AutoUpdater? autoUpdater;
      await using (autoUpdater = new AutoUpdater(Array.Empty<IAsyncProperty>()) { Delay = AutoUpdateDelay })
        autoUpdater.Start();

      Assert.False(autoUpdater.IsRunning);
      Assert.Throws<ObjectDisposedException>(autoUpdater.Start);
      Assert.Throws<ObjectDisposedException>(autoUpdater.Stop);
      await Assert.ThrowsAsync<ObjectDisposedException>(autoUpdater.StopAsync);

      autoUpdater.Dispose(); // Repeated object disposal should pass OK.
      await autoUpdater.DisposeAsync(); // Repeated object disposal should pass OK.
    }
  }
}
