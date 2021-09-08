// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;
using LocalizationResourceManager = System.Resources.ResourceManager;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The VISA device controller class. This class represents the abstraction layer between the user interface and
  ///   the VISA device control logic.
  /// </summary>
  public class VisaDeviceController : IVisaDeviceController
  {
    /// <summary>
    ///   The flag indicating if the controller is being disposed of at the moment.
    /// </summary>
    private bool _isDisposing;

    /// <summary>
    ///   The flag indicating if the controller has been already disposed of.
    /// </summary>
    private bool _isDisposed;

    /// <inheritdoc />
    public IVisaDevice Device { get; }

    /// <inheritdoc />
    public bool IsMessageDevice => Device is IMessageDevice;

    /// <summary>
    ///   Gets the mutable collection of asynchronous properties and corresponding metadata defined for the device.
    /// </summary>
    protected ObservableCollection<IAsyncProperty> AsyncPropertyEntries { get; } = new();

    /// <summary>
    ///   Gets the mutable collection of device actions and corresponding metadata defined for the device.
    /// </summary>
    protected ObservableCollection<IDeviceAction> DeviceActionEntries { get; } = new();

    /// <inheritdoc />
    public ReadOnlyObservableCollection<IAsyncProperty> AsyncProperties { get; }

    /// <inheritdoc />
    public ReadOnlyObservableCollection<IDeviceAction> DeviceActions { get; }

    /// <summary>
    ///   Gets the mutable collection of the available VISA resources.
    /// </summary>
    private ObservableCollection<string> VisaResourceEntries { get; } = new();

    /// <inheritdoc />
    public ReadOnlyObservableCollection<string> AvailableVisaResources { get; }

    /// <inheritdoc />
    public bool IsUpdatingVisaResources
    {
      get => _isUpdatingVisaResources;
      private set
      {
        _isUpdatingVisaResources = value;
        OnPropertyChanged();
      }
    }
    private bool _isUpdatingVisaResources = false;

    /// <inheritdoc />
    public IResourceManager? ResourceManager
    {
      get => Device.ResourceManager;
      set
      {
        Device.ResourceManager = value;
        OnPropertyChanged();
      }
    }

    /// <inheritdoc />
    public string ResourceName
    {
      get => Device.ResourceName;
      set
      {
        Device.ResourceName = value;
        OnPropertyChanged();
      }
    }

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
    private bool _canConnect = true;

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
    private bool _isDeviceReady = false;

    /// <inheritdoc />
    public bool IsUpdatingAsyncProperties
    {
      get => _isUpdatingAsyncProperties;
      private set
      {
        _isUpdatingAsyncProperties = value;
        OnPropertyChanged();
      }
    }
    private bool _isUpdatingAsyncProperties = false;

    /// <inheritdoc />
    public IAutoUpdater AutoUpdater { get; }

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
    private bool _isAutoUpdaterEnabled = true;

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

    /// <inheritdoc />
    public bool IsDisconnectionRequested
    {
      get => _isDisconnectionRequested;
      private set
      {
        _isDisconnectionRequested = value;
        OnPropertyChanged();
      }
    }
    private bool _isDisconnectionRequested = false;

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
    private string _identifier = string.Empty;

    /// <inheritdoc />
    public LocalizationResourceManager? LocalizationResourceManager
    {
      get => _localizationResourceManager;
      set
      {
        _localizationResourceManager = value;
        OnPropertyChanged();
        RebuildCollections();
      }
    }
    private LocalizationResourceManager? _localizationResourceManager;

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
      AvailableVisaResources = new(VisaResourceEntries);
      AsyncProperties = new(AsyncPropertyEntries);
      DeviceActions = new(DeviceActionEntries);

      DeviceActionExecutor.Exception += OnException;
      AutoUpdater.AutoUpdateException += OnException;

      RebuildCollections();
    }

    /// <summary>
    ///   Rebuilds the collections of asynchronous properties and device actions and localizes the names using the
    ///   specified <see cref="LocalizationResourceManager" />.
    ///   If <see cref="LocalizationResourceManager" /> is not provided, the original names are used.
    /// </summary>
    private void RebuildCollections()
    {
      AsyncPropertyEntries.Clear();
      DeviceActionEntries.Clear();

      foreach (var asyncProperty in Device.AsyncProperties)
      {
        asyncProperty.Name = LocalizationResourceManager?.GetString(asyncProperty.Name) ?? asyncProperty.Name;
        AsyncPropertyEntries.Add(asyncProperty);
      }

      foreach (var deviceAction in Device.DeviceActions)
      {
        deviceAction.Name = LocalizationResourceManager?.GetString(deviceAction.Name) ?? deviceAction.Name;
        DeviceActionEntries.Add(deviceAction);
      }
    }

    /// <inheritdoc />
    public virtual async Task UpdateResourcesListAsync()
    {
      if (_isDisposed)
        throw new ObjectDisposedException(nameof(VisaDeviceController));

      if (IsUpdatingVisaResources)
        return;
      IsUpdatingVisaResources = true;

      try
      {
        var resources = ResourceManager == null
          ? await VisaResourceLocator.LocateResourceNamesAsync()
          : await VisaResourceLocator.LocateResourceNamesAsync(ResourceManager);
        VisaResourceEntries.Clear();
        foreach (var resource in resources)
          VisaResourceEntries.Add(resource);
      }
      catch (Exception e)
      {
        OnException(e);
      }
      finally
      {
        IsUpdatingVisaResources = false;
      }
    }

    /// <inheritdoc />
    public void BeginConnect()
    {
      if (_isDisposed)
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

    /// <inheritdoc />
    public void BeginDisconnect()
    {
      if (_isDisposed)
        throw new ObjectDisposedException(nameof(VisaDeviceController));

      if (IsDisconnectionRequested || ConnectionTask == null || DisconnectionTokenSource == null)
        return;
      IsDisconnectionRequested = true;

      IsDeviceReady = false;
      Identifier = string.Empty;
      DisconnectionTask = DisconnectDeviceAsync();
    }

    /// <summary>
    ///   Creates and asynchronously runs the device disconnection <see cref="Task" /> that handles the entire VISA
    ///   device disconnection process.
    /// </summary>
    private async Task DisconnectDeviceAsync()
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
        await Task.WhenAll(AsyncProperties.SelectMany(property => new[]
        {
          property.GetGetterUpdatingTask(),
          property.GetGetterUpdatingTask()
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
            Device.CloseSession();
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
      if (_isDisposed)
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
    protected virtual void OnConnected() => Connected?.Invoke(this, new EventArgs());

    /// <summary>
    ///   Invokes the <see cref="Disconnected" /> event.
    /// </summary>
    protected virtual void OnDisconnected() => Disconnected?.Invoke(this, new EventArgs());

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
      if (_isDisposing || _isDisposed)
        return;
      _isDisposing = true;

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
        _isDisposed = true;
      }
    }

    /// <inheritdoc />
    public virtual void Dispose() => DisposeAsync().AsTask().Wait();

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    ~VisaDeviceController() => Dispose();
  }
}
