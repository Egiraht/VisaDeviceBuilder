using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;
using LocalizationResourceManager = System.Resources.ResourceManager;

namespace VisaDeviceBuilder
{
  // TODO: Analyze if the class is necessary (can its functions be transferred into VisaDevice or builders?).
  /// <summary>
  ///   The VISA device controller class. This class represents the abstraction layer between the user interface and
  ///   the VISA device control logic.
  /// </summary>
  public class VisaDeviceController : IVisaDeviceController
  {
    /// <summary>
    ///   Defines the default auto-updater delay value in milliseconds.
    /// </summary>
    /// <seealso cref="AutoUpdaterDelay" />
    public const int DefaultAutoUpdaterDelay = 10;

    /// <summary>
    ///   The flag indicating if the controller has been already disposed.
    /// </summary>
    private bool _isDisposed = false;

    /// <inheritdoc />
    public Type DeviceType
    {
      get => _deviceType;
      set
      {
        if (!value.IsClass || !typeof(IVisaDevice).IsAssignableFrom(value))
          throw new InvalidOperationException(
            $"The specified VISA device type \"{value.Name}\" does not implement the \"{nameof(IVisaDevice)}\" interface.");

        _deviceType = value;
        OnPropertyChanged();
      }
    }
    private Type _deviceType = typeof(VisaDevice);

    /// <inheritdoc />
    public Type? VisaResourceManagerType
    {
      get => _visaResourceManagerType;
      set
      {
        if (value != null && (!value.IsClass || !typeof(IResourceManager).IsAssignableFrom(value)))
        {
          throw new InvalidOperationException(
            $"The specified VISA resource manager type \"{value.Name}\" does not implement the \"{nameof(IResourceManager)}\" interface.");
        }

        _visaResourceManagerType = value;
        OnPropertyChanged();
      }
    }
    private Type? _visaResourceManagerType;

    /// <inheritdoc />
    public string ResourceName
    {
      get => _resourceName;
      set
      {
        _resourceName = value;
        OnPropertyChanged();
      }
    }
    private string _resourceName = string.Empty;

    /// <inheritdoc />
    public ObservableCollection<string> AvailableVisaResources { get; } = new();

    /// <inheritdoc />
    public bool CanConnect
    {
      get => _canConnect;
      protected set
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
      protected set
      {
        _isDeviceReady = value;
        OnPropertyChanged();
      }
    }
    private bool _isDeviceReady = false;

    /// <inheritdoc />
    public DeviceConnectionState ConnectionState => Device?.ConnectionState ?? _lastConnectionState;
    private DeviceConnectionState _lastConnectionState = DeviceConnectionState.Disconnected;

    /// <inheritdoc />
    public bool IsUpdatingVisaResources
    {
      get => _isUpdatingVisaResources;
      protected set
      {
        _isUpdatingVisaResources = value;
        OnPropertyChanged();
      }
    }
    private bool _isUpdatingVisaResources = false;

    /// <inheritdoc />
    public bool IsUpdatingAsyncProperties
    {
      get => _isUpdatingAsyncProperties;
      protected set
      {
        _isUpdatingAsyncProperties = value;
        OnPropertyChanged();
      }
    }
    private bool _isUpdatingAsyncProperties = false;

    /// <inheritdoc />
    public bool IsDisconnectionRequested
    {
      get => _isDisconnectionRequested;
      protected set
      {
        _isDisconnectionRequested = value;
        OnPropertyChanged();
      }
    }
    private bool _isDisconnectionRequested = false;

    /// <inheritdoc />
    public IVisaDevice? Device
    {
      get => _device;
      protected set
      {
        _device = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(IsMessageDevice));
      }
    }
    private IVisaDevice? _device;

    /// <inheritdoc />
    public bool IsMessageDevice => DeviceType.GetInterface(nameof(IMessageDevice)) != null;

    /// <inheritdoc />
    public string Identifier
    {
      get => _identifier;
      protected set
      {
        _identifier = value;
        OnPropertyChanged();
      }
    }
    private string _identifier = string.Empty;

    /// <summary>
    ///   Gets the collection of asynchronous properties and corresponding metadata defined for the device.
    /// </summary>
    protected ObservableCollection<IAsyncProperty> AsyncPropertyEntries { get; } = new();

    /// <summary>
    ///   Gets the collection of device actions and corresponding metadata defined for the device.
    /// </summary>
    protected ObservableCollection<IDeviceAction> DeviceActionEntries { get; } = new();

    /// <inheritdoc />
    public ReadOnlyObservableCollection<IAsyncProperty> AsyncProperties { get; }

    /// <inheritdoc />
    public ReadOnlyObservableCollection<IDeviceAction> DeviceActions { get; }

    /// <inheritdoc />
    public LocalizationResourceManager? LocalizationResourceManager
    {
      get => _localizationResourceManager;
      set
      {
        _localizationResourceManager = value;
        OnPropertyChanged();
      }
    }
    private LocalizationResourceManager? _localizationResourceManager;

    /// <summary>
    ///   Gets the auto-updater object that allows to automatically update getters of asynchronous properties
    ///   available in the <see cref="AsyncProperties" /> collection.
    /// </summary>
    private IAutoUpdater? AutoUpdater
    {
      get => _autoUpdater;
      set
      {
        _autoUpdater = value;
        OnPropertyChanged();
      }
    }
    private IAutoUpdater? _autoUpdater;

    /// <inheritdoc />
    public bool IsAutoUpdaterEnabled
    {
      get => _isAutoUpdaterEnabled;
      set
      {
        _isAutoUpdaterEnabled = value;
        OnPropertyChanged();

        if (AutoUpdater == null)
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
      get => _autoUpdaterDelay;
      set
      {
        _autoUpdaterDelay = value;
        OnPropertyChanged();

        if (AutoUpdater != null)
          AutoUpdater.Delay = TimeSpan.FromMilliseconds(_autoUpdaterDelay);
      }
    }
    private int _autoUpdaterDelay = DefaultAutoUpdaterDelay;

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
    public event EventHandler<DeviceConnectionState>? ConnectionStateChanged;

    /// <inheritdoc />
    public event EventHandler<IVisaDevice>? AutoUpdaterCycle;

    /// <inheritdoc />
    public event ThreadExceptionEventHandler? Exception;

    /// <summary>
    ///   Creates a new view-model instance.
    /// </summary>
    public VisaDeviceController()
    {
      AsyncProperties = new(AsyncPropertyEntries);
      DeviceActions = new(DeviceActionEntries);
      DeviceActionExecutor.Exception += OnException;
    }

    /// <inheritdoc />
    public virtual async Task UpdateResourcesListAsync()
    {
      if (IsUpdatingVisaResources)
        return;
      IsUpdatingVisaResources = true;

      try
      {
        var resources = VisaResourceManagerType == null
          ? await VisaResourceLocator.LocateResourceNamesAsync()
          : await VisaResourceLocator.LocateResourceNamesAsync(VisaResourceManagerType);
        AvailableVisaResources.Clear();
        foreach (var resource in resources)
          AvailableVisaResources.Add(resource);
      }
      catch (VisaException)
      {
        AvailableVisaResources.Clear();
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

    /// <summary>
    ///   Creates a new VISA device instance.
    /// </summary>
    /// <param name="deviceType">
    ///   The type of the VISA device to create an instance for.
    ///   The specified device type must implement the <see cref="IVisaDevice" /> interface and have a public
    ///   constructor accepting a resource name string and an optional <see cref="IResourceManager" /> instance.
    /// </param>
    /// <param name="resourceName">
    ///   The VISA resource name to create a new VISA device instance for.
    /// </param>
    /// <param name="resourceManager">
    ///   The optional instance of custom VISA resource manager.
    ///   If set to <c>null</c>, the default <see cref="GlobalResourceManager" /> static class will be used for
    ///   instance creation.
    /// </param>
    /// <returns>
    ///   A new <see cref="IVisaDevice" /> instance.
    /// </returns>
    protected static IVisaDevice CreateDeviceInstance(Type deviceType, string resourceName,
      IResourceManager? resourceManager)
    {
      var device = (IVisaDevice) Activator.CreateInstance(deviceType)!;
      device.ResourceName = resourceName;
      device.ResourceManager = resourceManager;
      return device;
    }

    /// <summary>
    ///   Rebuilds the collections of asynchronous properties and device actions and localizes the names using the
    ///   specified <see cref="LocalizationResourceManager" />.
    ///   If <see cref="LocalizationResourceManager" /> is not provided, the original names are used.
    /// </summary>
    protected virtual void RebuildCollections()
    {
      AsyncPropertyEntries.Clear();
      DeviceActionEntries.Clear();

      if (Device == null)
        return;

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
    public virtual void Connect()
    {
      if (!CanConnect)
        return;
      CanConnect = false;

      // Checking the VISA resource name.
      using (var visaResourceManager = VisaResourceManagerType != null
        ? (IResourceManager?) Activator.CreateInstance(VisaResourceManagerType)
        : null)
      {
        try
        {
          if (visaResourceManager != null)
            visaResourceManager.Parse(ResourceName);
          else
            GlobalResourceManager.Parse(ResourceName);
        }
        catch
        {
          return;
        }
      }

      ConnectionTask = CreateDeviceConnectionTask();
    }

    /// <summary>
    ///   Creates the asynchronous device connection <see cref="Task" /> that handles the entire device
    ///   connection and disconnection process.
    /// </summary>
    protected virtual async Task CreateDeviceConnectionTask()
    {
      try
      {
        using var visaResourceManager = VisaResourceManagerType != null
          ? (IResourceManager?) Activator.CreateInstance(VisaResourceManagerType)
          : null;

        await using (Device = CreateDeviceInstance(DeviceType, ResourceName, visaResourceManager))
        await using (AutoUpdater = new AutoUpdater(Device))
        using (DisconnectionTokenSource = new CancellationTokenSource())
        {
          // Trying to connect to the device.
          var disconnectionToken = DisconnectionTokenSource.Token;
          Device.ConnectionStateChanged += ConnectionStateChanged;
          await Device.OpenSessionAsync();

          // Rebuilding and localizing the collections of asynchronous properties and device actions.
          RebuildCollections();

          // Getting the device identifier string.
          disconnectionToken.ThrowIfCancellationRequested();
          Identifier = await Device.GetIdentifierAsync();

          // Trying to get the initial getter values of the asynchronous properties.
          // If any getter exception occurs on this stage, throw it and disconnect from the device.
          ThreadExceptionEventHandler throwOnGetterUpdate = (_, args) => throw args.Exception;
          foreach (var asyncProperty in Device.AsyncProperties)
          {
            disconnectionToken.ThrowIfCancellationRequested();
            asyncProperty.GetterException += throwOnGetterUpdate;
            asyncProperty.RequestGetterUpdate();
            await asyncProperty.GetGetterUpdatingTask();
            asyncProperty.GetterException -= throwOnGetterUpdate;
          }

          // Subscribing on further exception events of the asynchronous properties.
          foreach (var asyncProperty in Device.AsyncProperties)
          {
            asyncProperty.GetterException += OnException;
            asyncProperty.SetterException += OnException;
          }

          // Configuring the auto-updater.
          disconnectionToken.ThrowIfCancellationRequested();
          AutoUpdater.Delay = TimeSpan.FromMilliseconds(AutoUpdaterDelay);
          AutoUpdater.AutoUpdateCycle += OnAutoUpdaterCycle;
          AutoUpdater.AutoUpdateException += OnException;
          if (IsAutoUpdaterEnabled)
            AutoUpdater.Start();
          IsDeviceReady = true;

          // Waiting for the disconnection request.
          try
          {
            await Task.Delay(-1, disconnectionToken);
          }
          catch (OperationCanceledException)
          {
            // Suppress task cancellation exceptions.
          }

          // Waiting for the auto-updater to stop.
          IsDeviceReady = false;
          if (AutoUpdater != null)
            await AutoUpdater.StopAsync();

          // Waiting for the device actions to complete.
          await DeviceActionExecutor.WaitForAllActionsToCompleteAsync();

          // Waiting for all asynchronous properties processing to complete.
          foreach (var asyncProperty in AsyncProperties)
          {
            await asyncProperty.GetSetterProcessingTask();
            await asyncProperty.GetGetterUpdatingTask();
          }

          // Waiting for remaining asynchronous operations to complete before session closing.
          await Task.Run(() =>
          {
            lock (DisconnectionLock)
              Device.CloseSession();
          });
          Device.ConnectionStateChanged -= ConnectionStateChanged;
          _lastConnectionState = DeviceConnectionState.Disconnected;
        }
      }
      catch (OperationCanceledException)
      {
        // Suppress task cancellation exceptions.
      }
      catch (Exception e)
      {
        OnException(e);
        _lastConnectionState = DeviceConnectionState.DisconnectedWithError;
      }
      finally
      {
        IsDeviceReady = false;
        Identifier = string.Empty;
        DisconnectionTokenSource = null;
        AutoUpdater = null;
        Device = null;
        RebuildCollections();
        CanConnect = true;
      }
    }

    /// <inheritdoc />
    public virtual async Task DisconnectAsync()
    {
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
      if (Device?.IsSessionOpened != true || IsUpdatingAsyncProperties)
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
    ///   Invokes the <see cref="AutoUpdaterCycle" /> event.
    /// </summary>
    /// <param name="sender">
    ///   The event sender object.
    /// </param>
    /// <param name="args">
    ///   The event arguments object.
    /// </param>
    protected virtual void OnAutoUpdaterCycle(object? sender, EventArgs args) =>
      AutoUpdaterCycle?.Invoke(this, Device ?? throw new ArgumentNullException());

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
