// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The VISA device controller class. This class represents the abstraction layer between the user interface and
  ///   the VISA device control logic.
  /// </summary>
  public class VisaDeviceController : IVisaDeviceController
  {
    /// <summary>
    ///   The flag indicating if the controller has been already disposed of.
    /// </summary>
    protected bool IsDisposed;

    /// <inheritdoc />
    public IVisaDevice Device { get; }

    private bool _canConnect = true;

    /// <inheritdoc />
    public bool CanConnect
    {
      get => _canConnect;
      private set
      {
        _canConnect = value;
        OnPropertyChanged();
      }
    }

    private bool _isDeviceReady;

    /// <inheritdoc />
    public bool IsDeviceReady
    {
      get => _isDeviceReady;
      private set
      {
        _isDeviceReady = value;
        OnPropertyChanged();
      }
    }

    /// <inheritdoc />
    public IAutoUpdater AutoUpdater { get; }

    private bool _isAutoUpdaterEnabled = true;

    /// <inheritdoc />
    public bool IsAutoUpdaterEnabled
    {
      get => _isAutoUpdaterEnabled;
      set
      {
        _isAutoUpdaterEnabled = value;
        OnPropertyChanged();

        if (!IsDeviceReady)
          return;

        if (_isAutoUpdaterEnabled)
          AutoUpdater.Start();
        else
          AutoUpdater.StopAsync();
      }
    }

    /// <inheritdoc />
    public int AutoUpdaterDelay
    {
      get => (int) AutoUpdater.Delay.TotalMilliseconds;
      set
      {
        AutoUpdater.Delay = TimeSpan.FromMilliseconds(value);
        OnPropertyChanged();
      }
    }

    private string _identifier = string.Empty;

    /// <inheritdoc />
    public string Identifier
    {
      get => _identifier;
      private set
      {
        _identifier = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Checks if getters of the device's asynchronous properties are being updated at the moment after calling the
    ///   <see cref="UpdateAsyncPropertiesAsync" /> method.
    ///   This property is not influenced by the <see cref="AutoUpdater" />.
    /// </summary>
    protected bool IsUpdatingAsyncProperties { get; private set; }

    /// <summary>
    ///   Checks if the device disconnection has been requested using the <see cref="BeginDisconnect" /> method.
    /// </summary>
    protected bool IsDisconnectionRequested { get; private set; }

    /// <summary>
    ///   Gets or sets the VISA device connection process <see cref="Task" />.
    /// </summary>
    private Task? ConnectionTask { get; set; }

    /// <summary>
    ///   Gets or sets the VISA device disconnection process <see cref="Task" />.
    /// </summary>
    private Task? DisconnectionTask { get; set; }

    /// <summary>
    ///   Stores the cancellation token source that allows to stop the device connection task and disconnect the device.
    /// </summary>
    private CancellationTokenSource? DisconnectionTokenSource { get; set; }

    /// <summary>
    ///   Gets the shared locking object used for device disconnection synchronization.
    /// </summary>
    protected object DisconnectionLock { get; } = new();

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public event EventHandler? Connected;

    /// <inheritdoc />
    public event EventHandler? Disconnected;

    /// <inheritdoc />
    public event ThreadExceptionEventHandler? Exception;

    /// <summary>
    ///   Creates a new VISA device controller instance.
    /// </summary>
    /// <param name="device">
    ///   The <see cref="IVisaDevice" /> object to be used for establishing a connection with the connected VISA device.
    /// </param>
    public VisaDeviceController(IVisaDevice device)
    {
      Device = device;
      AutoUpdater = new AutoUpdater(Device);

      DeviceActionExecutor.Exception += OnException;
      AutoUpdater.AutoUpdateException += OnException;
    }

    /// <inheritdoc />
    public void BeginConnect()
    {
      if (IsDisposed)
        throw new ObjectDisposedException(nameof(VisaDeviceController));

      if (!CanConnect)
        return;
      CanConnect = false;

      DisconnectionTokenSource = new CancellationTokenSource();
      ConnectionTask = ConnectDeviceAsync(DisconnectionTokenSource.Token);
    }

    /// <summary>
    ///   Handles the <see cref="IAsyncProperty.GetterException" /> event and throws the exception provided in the event
    ///   arguments.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static void ThrowOnGetterException(object sender, ThreadExceptionEventArgs args) => throw args.Exception;

    /// <summary>
    ///   Creates and asynchronously runs the device connection <see cref="Task" /> that handles the entire VISA device
    ///   connection process.
    /// </summary>
    private async Task ConnectDeviceAsync(CancellationToken cancellationToken)
    {
      try
      {
        // Trying to open a new VISA session with the device.
        await Device.OpenSessionAsync();

        // Getting the device identifier string.
        cancellationToken.ThrowIfCancellationRequested();
        Identifier = await Device.GetIdentifierAsync();

        // Trying to get the initial getter values of the asynchronous properties.
        // If any getter exception occurs on this stage, throw it and disconnect from the device.
        foreach (var asyncProperty in Device.AsyncProperties)
        {
          cancellationToken.ThrowIfCancellationRequested();
          asyncProperty.GetterException += ThrowOnGetterException;
          asyncProperty.RequestGetterUpdate();
          await asyncProperty.GetGetterUpdatingTask();
          asyncProperty.GetterException -= ThrowOnGetterException;
        }

        // Resubscribing on further exceptions of asynchronous properties using the OnException handler that does not
        // cause disconnection.
        foreach (var asyncProperty in Device.AsyncProperties)
        {
          asyncProperty.GetterException += OnException;
          asyncProperty.SetterException += OnException;
        }

        // Starting the auto-updater.
        cancellationToken.ThrowIfCancellationRequested();
        if (IsAutoUpdaterEnabled)
          AutoUpdater.Start();
        IsDeviceReady = true;

        OnConnected();
      }
      catch (OperationCanceledException)
      {
        // Suppress task cancellation exceptions.
      }
      catch (Exception e)
      {
        try
        {
          OnException(e);
        }
        finally
        {
          BeginDisconnect();
        }
      }
    }

    /// <inheritdoc />
    public Task GetDeviceConnectionTask() => ConnectionTask ?? Task.CompletedTask;

    /// <inheritdoc cref="BeginDisconnect" />
    /// <param name="withError">
    ///   The flag defining if the disconnection was caused by an error.
    /// </param>
    protected void BeginDisconnect(bool withError)
    {
      if (IsDisposed)
        throw new ObjectDisposedException(nameof(VisaDeviceController));

      if (IsDisconnectionRequested || ConnectionTask == null || DisconnectionTokenSource == null)
        return;
      IsDisconnectionRequested = true;

      IsDeviceReady = false;
      Identifier = string.Empty;
      DisconnectionTask = DisconnectDeviceAsync(withError);
    }

    /// <inheritdoc />
    public void BeginDisconnect() => BeginDisconnect(false);

    /// <summary>
    ///   Creates and asynchronously runs the device disconnection <see cref="Task" /> that handles the entire VISA
    ///   device disconnection process.
    /// </summary>
    /// <param name="withError">
    ///   The flag defining if the disconnection was caused by an error.
    /// </param>
    private async Task DisconnectDeviceAsync(bool withError)
    {
      try
      {
        // Cancelling the connection task if possible.
        try
        {
          DisconnectionTokenSource!.Cancel();
          await ConnectionTask!;
        }
        catch
        {
          // Ignore exceptions.
        }

        // Waiting for the auto-updater to stop.
        await AutoUpdater.StopAsync();

        // Waiting for the device actions to complete.
        await DeviceActionExecutor.WaitForAllActionsToCompleteAsync();

        // Waiting for all asynchronous properties processing to complete.
        await Task.WhenAll(Device.AsyncProperties.SelectMany(property => new[]
        {
          property.GetGetterUpdatingTask(),
          property.GetSetterProcessingTask()
        }));

        // Unsubscribing from exceptions of asynchronous properties.
        foreach (var asyncProperty in Device.AsyncProperties)
        {
          asyncProperty.GetterException -= OnException;
          asyncProperty.SetterException -= OnException;
        }

        // Waiting for remaining asynchronous operations to complete before session closing.
        await Task.Run(() =>
        {
          lock (DisconnectionLock)
          {
            if (withError)
              Device.CloseSessionWithError();
            else
              Device.CloseSession();
          }
        });
      }
      catch (Exception e)
      {
        OnException(e);
      }
      finally
      {
        ConnectionTask?.Dispose();
        ConnectionTask = null;
        DisconnectionTokenSource?.Dispose();
        DisconnectionTokenSource = null;
        IsDisconnectionRequested = false;
        CanConnect = true;

        OnDisconnected();
      }
    }

    /// <inheritdoc />
    public Task GetDeviceDisconnectionTask() => DisconnectionTask ?? Task.CompletedTask;

    /// <inheritdoc />
    /// <exception cref="VisaDeviceException">
    ///   There is no opened VISA session to perform an operation.
    /// </exception>
    public virtual async Task UpdateAsyncPropertiesAsync()
    {
      if (IsDisposed)
        throw new ObjectDisposedException(nameof(VisaDeviceController));

      if (IsUpdatingAsyncProperties)
        return;
      IsUpdatingAsyncProperties = true;

      try
      {
        if (!Device.IsSessionOpened)
          throw new VisaDeviceException(Device,
            new InvalidOperationException("There is no opened VISA session to perform an operation."));

        await Task.Run(() =>
        {
          lock (DisconnectionLock)
          {
            foreach (var asyncProperty in Device.AsyncProperties)
            {
              if (IsDisconnectionRequested)
                return;

              asyncProperty.RequestGetterUpdate();
              asyncProperty.GetGetterUpdatingTask().Wait();
            }
          }
        });
      }
      catch (Exception e)
      {
        OnException(e);
      }
      finally
      {
        IsUpdatingAsyncProperties = false;
      }
    }

    /// <summary>
    ///   Invokes the <see cref="PropertyChanged" /> event.
    /// </summary>
    /// <param name="propertyName">
    ///   Name of the property being changed.
    ///   If set to <c>null</c> the caller member name is used.
    /// </param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    ///   Invokes the <see cref="Connected" /> event.
    /// </summary>
    protected virtual void OnConnected() => Connected?.Invoke(this, EventArgs.Empty);

    /// <summary>
    ///   Invokes the <see cref="Disconnected" /> event.
    /// </summary>
    protected virtual void OnDisconnected() => Disconnected?.Invoke(this, EventArgs.Empty);

    /// <summary>
    ///   Invokes the <see cref="Exception" /> event.
    /// </summary>
    /// <param name="exception">
    ///   The exception instance to be provided with the event.
    /// </param>
    protected virtual void OnException(Exception exception) =>
      OnException(this, new ThreadExceptionEventArgs(exception is VisaDeviceException visaDeviceException
        ? visaDeviceException
        : new VisaDeviceException(Device, exception)));

    /// <summary>
    ///   Invokes the <see cref="Exception" /> event.
    /// </summary>
    /// <param name="sender">
    ///   The event sender object.
    /// </param>
    /// <param name="args">
    ///   The event arguments object containing the thrown exception.
    /// </param>
    protected virtual void OnException(object? sender, ThreadExceptionEventArgs args) => Exception?.Invoke(this, args);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
      if (IsDisposed)
        return;

      try
      {
        BeginDisconnect();
        await GetDeviceDisconnectionTask();
        AutoUpdater.Dispose();
      }
      catch
      {
        // Suppress possible exceptions.
      }
      finally
      {
        DeviceActionExecutor.Exception -= OnException;
        GC.SuppressFinalize(this);
        IsDisposed = true;
      }
    }

    /// <inheritdoc />
    public virtual void Dispose() => DisposeAsync().AsTask().Wait();

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    ~VisaDeviceController() => Dispose();
  }
}
