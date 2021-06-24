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
    ///   The flag indicating if the controller has been already disposed.
    /// </summary>
    private bool _isDisposed = false;

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

    /// <inheritdoc cref="IVisaDeviceController.AutoUpdater" />
    private IAutoUpdater AutoUpdater { get; }

    /// <inheritdoc />
    IAutoUpdater IVisaDeviceController.AutoUpdater => AutoUpdater;

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
    ///   Stores the <see cref="Task" /> for the established device connection.
    /// </summary>
    protected Task? ConnectionTask { get; set; }

    /// <summary>
    ///   Stores the cancellation token source that allows to stop the device connection task and disconnect the device.
    /// </summary>
    protected CancellationTokenSource? DisconnectionTokenSource { get; set; }

    /// <summary>
    ///   Gets the shared locking object used for device disconnection synchronization.
    /// </summary>
    protected object DisconnectionLock { get; } = new();

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

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
        var resources = Device.ResourceManager == null
          ? await VisaResourceLocator.LocateResourceNamesAsync()
          : await VisaResourceLocator.LocateResourceNamesAsync(Device.ResourceManager);
        VisaResourceEntries.Clear();
        foreach (var resource in resources)
          VisaResourceEntries.Add(resource);
      }
      catch (VisaException)
      {
        VisaResourceEntries.Clear();
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
    public virtual void Connect()
    {
      if (_isDisposed)
        throw new ObjectDisposedException(nameof(VisaDeviceController));

      if (!CanConnect)
        return;
      CanConnect = false;

      // Checking the VISA resource name.
      try
      {
        if (Device.ResourceManager != null)
          Device.ResourceManager.Parse(ResourceName);
        else
          GlobalResourceManager.Parse(ResourceName);
      }
      catch
      {
        OnException(new VisaDeviceException(Device,
          $"Cannot find a VISA resource with the name {ResourceName} using the VISA resource manager of type \"" +
          (Device.ResourceManager != null ? Device.ResourceManager.GetType().Name : nameof(GlobalResourceManager)) +
          "\"."));
      }

      ConnectionTask = CreateDeviceConnectionTask();
    }

    /// <summary>
    ///   Creates the asynchronous device connection <see cref="Task" /> that handles the entire device
    ///   connection and disconnection process.
    /// </summary>
    private async Task CreateDeviceConnectionTask()
    {
      // Catching all connection task exceptions.
      try
      {
        // Catching the disconnection token.
        try
        {
          // Trying to connect to the device.
          DisconnectionTokenSource = new CancellationTokenSource();
          var disconnectionToken = DisconnectionTokenSource.Token;
          await Device.OpenSessionAsync();

          // Getting the device identifier string.
          disconnectionToken.ThrowIfCancellationRequested();
          Identifier = await Device.GetIdentifierAsync();

          // Trying to get the initial getter values of the asynchronous properties.
          // If any getter exception occurs on this stage, throw it and disconnect from the device.
          static void ThrowOnGetterException(object _, ThreadExceptionEventArgs args) => throw args.Exception;
          foreach (var asyncProperty in Device.AsyncProperties)
          {
            disconnectionToken.ThrowIfCancellationRequested();

            // Subscribing on the initial asynchronous property's getter exception to cause disconnection.
            asyncProperty.GetterException += ThrowOnGetterException;
            asyncProperty.RequestGetterUpdate();
            await asyncProperty.GetGetterUpdatingTask();
            asyncProperty.GetterException -= ThrowOnGetterException;

            // Resubscribing on further asynchronous property's exceptions using the default handler that does not cause
            // a disconnection.
            asyncProperty.GetterException += OnException;
            asyncProperty.SetterException += OnException;
          }

          // Configuring the auto-updater.
          disconnectionToken.ThrowIfCancellationRequested();
          if (IsAutoUpdaterEnabled)
            AutoUpdater.Start();
          IsDeviceReady = true;

          // Waiting for the disconnection request.
          await Task.Delay(-1, disconnectionToken);
        }
        catch (OperationCanceledException)
        {
          // Suppress task cancellation exceptions.
        }
        finally
        {
          DisconnectionTokenSource?.Dispose();
          DisconnectionTokenSource = null;
          Identifier = string.Empty;
          IsDeviceReady = false;
        }

        // Waiting for the auto-updater to stop.
        await AutoUpdater.StopAsync();

        // Waiting for the device actions to complete.
        await DeviceActionExecutor.WaitForAllActionsToCompleteAsync();

        // Waiting for all asynchronous properties processing to complete.
        await Task.WhenAll(AsyncProperties.SelectMany(property =>
          new[] {property.GetGetterUpdatingTask(), property.GetGetterUpdatingTask()}));

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
        CanConnect = true;
      }
    }

    /// <inheritdoc />
    public virtual async Task DisconnectAsync()
    {
      if (_isDisposed)
        throw new ObjectDisposedException(nameof(VisaDeviceController));

      if (IsDisconnectionRequested || ConnectionTask == null || DisconnectionTokenSource == null)
        return;
      IsDisconnectionRequested = true;

      try
      {
        DisconnectionTokenSource.Cancel();
        await ConnectionTask;
      }
      finally
      {
        ConnectionTask.Dispose();
        ConnectionTask = null;
        IsDisconnectionRequested = false;
      }
    }

    /// <inheritdoc />
    public virtual async Task UpdateAsyncPropertiesAsync()
    {
      if (_isDisposed)
        throw new ObjectDisposedException(nameof(VisaDeviceController));

      if (Device.IsSessionOpened != true || IsUpdatingAsyncProperties)
        return;
      IsUpdatingAsyncProperties = true;

      try
      {
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
      if (_isDisposed)
        return;

      try
      {
        await DisconnectAsync();
        DeviceActionExecutor.Exception -= OnException;

        AutoUpdater.Dispose();
        Device.Dispose();
      }
      catch
      {
        // Suppress possible exceptions.
      }
      finally
      {
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
