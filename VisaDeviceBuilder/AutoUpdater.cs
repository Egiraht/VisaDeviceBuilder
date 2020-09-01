using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    }

    /// <summary>
    ///   Creates a new instance of auto-updater.
    /// </summary>
    /// <param name="visaDevice">
    ///   The VISA device object which asynchronous properties should be updated by the created auto-updater instance.
    /// </param>
    public AutoUpdater(IVisaDevice visaDevice) : this(visaDevice.AsyncProperties.Values)
    {
    }

    /// <summary>
    ///   The callback action representing the asynchronous auto-update loop.
    /// </summary>
    protected virtual async Task AutoUpdateLoopAsync()
    {
      if (!AsyncProperties.Any() || CancellationTokenSource == null)
        return;

      while (!CancellationTokenSource.IsCancellationRequested)
      {
        foreach (var property in AsyncProperties)
        {
          if (CancellationTokenSource.IsCancellationRequested)
            break;

          property.GetterException += AutoUpdateException;
          await property.UpdateGetterAsync();
          property.GetterException -= AutoUpdateException;
        }

        AutoUpdateCycle?.Invoke(this, EventArgs.Empty);

        await Task.Delay(Delay, CancellationTokenSource.Token);
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
      AutoUpdaterTask = Task.Run(AutoUpdateLoopAsync, CancellationTokenSource.Token);
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

      AutoUpdaterTask.Dispose();
      AutoUpdaterTask = null;
      CancellationTokenSource.Dispose();
      CancellationTokenSource = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
      if (_disposed)
        return;

      Stop();

      GC.SuppressFinalize(this);
      _disposed = true;
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    ~AutoUpdater()
    {
      Dispose();
    }
  }
}
