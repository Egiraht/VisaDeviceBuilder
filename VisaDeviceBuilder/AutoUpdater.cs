// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The auto-updater class that periodically updates the getter values of asynchronous properties within
  ///   the provided collection.
  /// </summary>
  public class AutoUpdater : IAutoUpdater
  {
    /// <summary>
    ///   The default delay value awaited before updating the next available asynchronous property.
    /// </summary>
    public static readonly TimeSpan DefaultDelay = TimeSpan.FromMilliseconds(10);

    /// <summary>
    ///   The object's disposal flag.
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    ///   The auto-update cycle cancellation token source.
    /// </summary>
    protected CancellationTokenSource? CancellationTokenSource { get; set; }

    /// <summary>
    ///   The <see cref="Task" /> encapsulating the asynchronous auto-updater logic.
    /// </summary>
    protected Task? AutoUpdaterTask { get; set; }

    /// <inheritdoc />
    public IEnumerable<IAsyncProperty> AsyncProperties { get; }

    /// <inheritdoc />
    public TimeSpan Delay { get; set; } = DefaultDelay;

    /// <inheritdoc />
    public bool IsRunning => AutoUpdaterTask != null;

    /// <inheritdoc />
    /// <remarks>
    ///   Do not call the <see cref="Stop" /> method inside the event handler because a deadlock will occur.
    /// </remarks>
    public event EventHandler? AutoUpdateCycle;

    /// <inheritdoc />
    public event ThreadExceptionEventHandler? AutoUpdateException;

    /// <summary>
    ///   Creates a new instance of auto-updater.
    /// </summary>
    /// <param name="asyncProperties">
    ///   The collection of asynchronous properties that should be updated by the created auto-updater instance.
    /// </param>
    public AutoUpdater(IEnumerable<IAsyncProperty> asyncProperties)
    {
      AsyncProperties = asyncProperties;
      foreach (var property in AsyncProperties)
        property.GetterException += OnAutoUpdateException;
    }

    /// <summary>
    ///   Creates a new instance of auto-updater.
    /// </summary>
    /// <param name="visaDevice">
    ///   The VISA device object which asynchronous properties should be updated by the created auto-updater instance.
    /// </param>
    public AutoUpdater(IVisaDevice visaDevice) : this(visaDevice.AsyncProperties)
    {
    }

    /// <summary>
    ///   The callback action representing the asynchronous auto-update loop.
    /// </summary>
    /// <param name="cancellationToken">
    ///   The cancellation token used to stop the auto-update loop.
    /// </param>
    protected virtual async Task AutoUpdateLoopAsync(CancellationToken cancellationToken)
    {
      while (!cancellationToken.IsCancellationRequested)
      {
        try
        {
          foreach (var property in AsyncProperties)
          {
            property.RequestGetterUpdate();
            await property.GetGetterUpdatingTask();
            cancellationToken.ThrowIfCancellationRequested();
          }

          OnAutoUpdateCycle();
          await Task.Delay(Delay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
          // Suppress task cancellation exceptions.
        }
        catch (Exception e)
        {
          OnAutoUpdateException(e);
        }
      }
    }

    /// <inheritdoc />
    public void Start()
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(AutoUpdater));

      if (!AsyncProperties.Any() || AutoUpdaterTask != null)
        return;

      CancellationTokenSource = new CancellationTokenSource();
      AutoUpdaterTask = Task.Run(() => AutoUpdateLoopAsync(CancellationTokenSource.Token),
        CancellationTokenSource.Token);
    }

    /// <inheritdoc />
    public void Stop()
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(AutoUpdater));

      if (AutoUpdaterTask == null || CancellationTokenSource == null)
        return;

      try
      {
        CancellationTokenSource.Cancel();
        AutoUpdaterTask.Wait();
      }
      catch
      {
        // Suppress task cancellation exceptions.
      }
      finally
      {
        AutoUpdaterTask.Dispose();
        AutoUpdaterTask = null;
        CancellationTokenSource.Dispose();
        CancellationTokenSource = null;
      }
    }

    /// <inheritdoc />
    public Task StopAsync() => Task.Run(Stop);

    /// <summary>
    ///   Invokes the <see cref="AutoUpdateCycle" /> event.
    /// </summary>
    protected virtual void OnAutoUpdateCycle() => AutoUpdateCycle?.Invoke(this, EventArgs.Empty);

    /// <summary>
    ///   Invokes the <see cref="AutoUpdateException" /> event.
    /// </summary>
    /// <param name="sender">
    ///   The event sender object.
    /// </param>
    /// <param name="args">
    ///   The event arguments object containing the thrown exception.
    /// </param>
    protected virtual void OnAutoUpdateException(object sender, ThreadExceptionEventArgs args) =>
      AutoUpdateException?.Invoke(sender, args);

    /// <summary>
    ///   Invokes the <see cref="AutoUpdateException" /> event.
    /// </summary>
    /// <param name="exception">
    ///   The exception instance.
    /// </param>
    protected virtual void OnAutoUpdateException(Exception exception) =>
      OnAutoUpdateException(this, new ThreadExceptionEventArgs(exception));

    /// <inheritdoc />
    public void Dispose()
    {
      if (_disposed)
        return;

      Stop();

      foreach (var property in AsyncProperties)
        property.GetterException -= OnAutoUpdateException;

      GC.SuppressFinalize(this);
      _disposed = true;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => new(Task.Run(Dispose));

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    ~AutoUpdater()
    {
      Dispose();
    }
  }
}
