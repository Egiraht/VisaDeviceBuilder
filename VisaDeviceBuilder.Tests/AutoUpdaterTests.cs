using System;
using System.Threading.Tasks;
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
    private const int CyclesCount = 3;

    /// <summary>
    ///   Defines the message for the test exception.
    /// </summary>
    private const string TestExceptionMessage = "Test exception message";

    /// <summary>
    ///   Defines the auto-update loop delay.
    /// </summary>
    private static readonly TimeSpan AutoUpdateDelay = TimeSpan.FromMilliseconds(1);

    /// <summary>
    ///   Testing the auto-update loop running for the test device.
    /// </summary>
    [Fact]
    public async Task DeviceAutoUpdateLoopTest()
    {
      using var resourceManager = new TestResourceManager();
      await using var device = new TestMessageDevice(TestResourceManager.SerialTestDeviceResourceName, resourceManager);
      await using var autoUpdater = new AutoUpdater(device) {Delay = AutoUpdateDelay};
      Assert.False(autoUpdater.IsRunning);
      Assert.Equal(device.AsyncProperties, autoUpdater.AsyncProperties);
      Assert.Equal(default, device.TestAsyncProperty.Getter);

      var cycleCounter = 0;
      autoUpdater.AutoUpdateCycle += (sender, args) => cycleCounter++;
      autoUpdater.Start();
      autoUpdater.Start();
      Assert.True(autoUpdater.IsRunning);

      while (cycleCounter < CyclesCount)
        await Task.Delay(AutoUpdateDelay);
      await autoUpdater.StopAsync();
      await autoUpdater.StopAsync();
      Assert.False(autoUpdater.IsRunning);
    }

    /// <summary>
    ///   Testing the auto-update loop cancellation when no asynchronous properties are available.
    /// </summary>
    [Fact]
    public void AutoUpdateLoopCancellationTest()
    {
      using var autoUpdater = new AutoUpdater(Array.Empty<IAsyncProperty>()) {Delay = AutoUpdateDelay};
      autoUpdater.Start();
      Assert.False(autoUpdater.IsRunning);
    }

    /// <summary>
    ///   Testing the auto-updater loop exception handling.
    /// </summary>
    [Fact]
    public async Task AutoUpdaterLoopExceptionTest()
    {
      // Testing the exception thrown when updating the asynchronous property getter.
      var exception = (Exception?) null;
      var testGetAsyncProperty = new AsyncProperty(() => throw new Exception(TestExceptionMessage));
      await using (var autoUpdater = new AutoUpdater(new[] {testGetAsyncProperty})
        {Delay = AutoUpdateDelay})
      {
        autoUpdater.AutoUpdateException += (sender, args) => exception = args.Exception;
        Assert.Contains(testGetAsyncProperty, autoUpdater.AsyncProperties);
        Assert.Null(exception);

        autoUpdater.Start();
        Assert.True(autoUpdater.IsRunning);

        while (exception == null)
          await Task.Delay(AutoUpdateDelay);
        Assert.Equal(TestExceptionMessage, exception?.Message);
      }

      // Testing the exception thrown when processing the auto-update cycle event callback.
      exception = null;
      testGetAsyncProperty = new AsyncProperty(() => string.Empty);
      await using (var autoUpdater = new AutoUpdater(new[] {testGetAsyncProperty})
        {Delay = AutoUpdateDelay})
      {
        autoUpdater.AutoUpdateException += (sender, args) => exception = args.Exception;
        autoUpdater.AutoUpdateCycle += (sender, args) => throw new Exception(TestExceptionMessage);
        Assert.Contains(testGetAsyncProperty, autoUpdater.AsyncProperties);
        Assert.Null(exception);

        autoUpdater.Start();
        Assert.True(autoUpdater.IsRunning);

        while (exception == null)
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
      AutoUpdater? autoUpdaterReference;
      await using (var autoUpdater = new AutoUpdater(Array.Empty<IAsyncProperty>()) {Delay = AutoUpdateDelay})
      {
        autoUpdaterReference = autoUpdater;
        autoUpdater.Start();
      }

      Assert.False(autoUpdaterReference.IsRunning);
      Assert.Throws<ObjectDisposedException>(autoUpdaterReference.Start);
      await Assert.ThrowsAsync<ObjectDisposedException>(autoUpdaterReference.StopAsync);

      // Testing the repeated object disposal.
      await autoUpdaterReference.DisposeAsync();
    }
  }
}
