using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ivi.Visa;
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
    ///   Defines the default auto-updater delay value in milliseconds.
    /// </summary>
    /// <seealso cref="AutoUpdaterDelay" />
    public const int DefaultAutoUpdaterDelay = 10;

    /* The private backing fields. */
    private bool _isDisposed = false;
    private Type _deviceType = typeof(VisaDevice);
    private Type? _visaResourceManagerType;
    private bool _canConnect = true;
    private bool _isDeviceReady = false;
    private string _resourceName = string.Empty;
    private bool _isUpdatingVisaResources = false;
    private bool _isUpdatingAsyncProperties = false;
    private bool _isDisconnectionRequested = false;
    private DeviceConnectionState _connectionState;
    private string _identifier = string.Empty;
    private IVisaDevice? _device;
    private LocalizationResourceManager? _localizationResourceManager;
    private IAutoUpdater? _autoUpdater;
    private bool _isAutoUpdaterEnabled = true;
    private int _autoUpdaterDelay = DefaultAutoUpdaterDelay;

    /// <inheritdoc />
    public Type DeviceType
    {
      get => _deviceType;
      set
      {
        if (!value.IsClass || !typeof(IVisaDevice).IsAssignableFrom(value))
        {
          OnException(new InvalidOperationException(
            $"The specified VISA device type \"{value.Name}\" does not implement the \"{nameof(IVisaDevice)}\" interface."));
          return;
        }

        _deviceType = value;
        OnPropertyChanged();
      }
    }

    /// <inheritdoc />
    public Type? VisaResourceManagerType
    {
      get => _visaResourceManagerType;
      set
      {
        if (value != null && (!value.IsClass || !typeof(IResourceManager).IsAssignableFrom(value)))
        {
          OnException(new InvalidOperationException(
            $"The specified VISA resource manager type \"{value.Name}\" does not implement the \"{nameof(IResourceManager)}\" interface."));
          return;
        }

        _visaResourceManagerType = value;
        OnPropertyChanged();
      }
    }

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

    /// <inheritdoc />
    public ObservableCollection<string> AvailableVisaResources { get; } = new ObservableCollection<string>();

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

    /// <inheritdoc />
    public DeviceConnectionState ConnectionState
    {
      get => _connectionState;
      protected set
      {
        _connectionState = value;
        OnPropertyChanged();
      }
    }

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

    /// <inheritdoc />
    public bool IsMessageDevice => Device is IMessageDevice;

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

    /// <inheritdoc />
    public ObservableCollection<AsyncPropertyMetadata> AsyncProperties { get; } =
      new ObservableCollection<AsyncPropertyMetadata>();

    /// <inheritdoc />
    public ObservableCollection<DeviceActionMetadata> DeviceActions { get; } =
      new ObservableCollection<DeviceActionMetadata>();

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
    protected object DisconnectionLock { get; } = new object();

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public event ThreadExceptionEventHandler? Exception;

    /// <summary>
    ///   Creates a new view-model instance.
    /// </summary>
    public VisaDeviceController() => DeviceActionExecutor.Exception += OnException;

    /// <inheritdoc />
    public virtual async Task UpdateResourcesListAsync()
    {
      try
      {
        if (IsUpdatingVisaResources)
          return;
        IsUpdatingVisaResources = true;

        // Searching for resources and aliases using the selected VISA resource manager.
        var resources = await Task.Run(() =>
        {
          using var visaResourceManager = VisaResourceManagerType != null
            ? (IResourceManager?) Activator.CreateInstance(VisaResourceManagerType)
            : null;
          return (visaResourceManager != null
              ? visaResourceManager.Find("?*::INSTR")
              : GlobalResourceManager.Find("?*::INSTR"))
            .Aggregate(new List<string>(), (results, resource) =>
            {
              var parseResult = visaResourceManager != null
                ? visaResourceManager.Parse(resource)
                : GlobalResourceManager.Parse(resource);
              results.Add(string.IsNullOrWhiteSpace(parseResult.AliasIfExists)
                ? parseResult.OriginalResourceName
                : parseResult.AliasIfExists);
              return results;
            });
        });

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
    /// <param name="resourceName">
    ///   The VISA resource name to create a new VISA device instance for.
    /// </param>
    /// <param name="deviceType">
    ///   The type of the VISA device to create an instance for.
    ///   The specified device type must implement the <see cref="IVisaDevice" /> interface and have a public
    ///   constructor accepting a resource name string and an optional <see cref="IResourceManager" /> instance.
    /// </param>
    /// <param name="resourceManager">
    ///   The optional instance of custom VISA resource manager.
    ///   If set to <c>null</c>, the default <see cref="GlobalResourceManager" /> static class will be used for
    ///   instance creation.
    /// </param>
    /// <returns>
    ///   A new <see cref="IVisaDevice" /> instance.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   The provided <paramref name="deviceType" /> does not implement the <see cref="IVisaDevice" /> interface.
    /// </exception>
    /// <exception cref="NotSupportedException">
    ///   Cannot create a new VISA device instance of the provided <paramref name="deviceType" /> as it does not
    ///   have a suitable constructor.
    /// </exception>
    protected static IVisaDevice CreateDeviceInstance(string resourceName, Type deviceType,
      IResourceManager? resourceManager)
    {
      if (!deviceType.IsClass || !typeof(IVisaDevice).IsAssignableFrom(deviceType))
        throw new InvalidOperationException(
          $"The specified VISA device type \"{deviceType.Name}\" does not implement the \"{nameof(IVisaDevice)}\" interface.");

      return (IVisaDevice) (Activator.CreateInstance(deviceType, resourceName, resourceManager) ??
        throw new NotSupportedException($"Cannot create a new VISA device instance of type \"{deviceType.Name}\"."));
    }

    /// <summary>
    ///   Rebuilds the collections of asynchronous properties and device actions and localizes the names using the
    ///   specified <see cref="LocalizationResourceManager" />.
    ///   If <see cref="LocalizationResourceManager" /> is not provided, the original names are used.
    /// </summary>
    protected virtual void RebuildCollections()
    {
      AsyncProperties.Clear();
      DeviceActions.Clear();

      if (Device == null)
        return;

      foreach (var (name, asyncProperty) in Device.AsyncProperties)
        AsyncProperties.Add(new AsyncPropertyMetadata
        {
          OriginalName = name,
          LocalizedName = LocalizationResourceManager?.GetString(name) ?? name,
          AsyncProperty = asyncProperty
        });

      foreach (var (name, deviceAction) in Device.DeviceActions)
        DeviceActions.Add(new DeviceActionMetadata
        {
          OriginalName = name,
          LocalizedName = LocalizationResourceManager?.GetString(name) ?? name,
          DeviceAction = deviceAction
        });
    }

    /// <inheritdoc />
    public virtual void Connect()
    {
      if (!CanConnect)
        return;

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
      if (!CanConnect)
        return;

      try
      {
        using (var visaResourceManager = VisaResourceManagerType != null
          ? (IResourceManager?) Activator.CreateInstance(VisaResourceManagerType)
          : null)
        await using (Device = CreateDeviceInstance(ResourceName, DeviceType, visaResourceManager))
        await using (AutoUpdater = new AutoUpdater(Device))
        using (DisconnectionTokenSource = new CancellationTokenSource())
        {
          var disconnectionToken = DisconnectionTokenSource.Token;
          CanConnect = false;
          ConnectionState = DeviceConnectionState.Initializing;

          // Trying to connect to the device.
          disconnectionToken.ThrowIfCancellationRequested();
          var sessionOpeningTask = Device.OpenSessionAsync();
          await sessionOpeningTask;

          // Rebuilding and localizing the collections of asynchronous properties and device actions.
          RebuildCollections();

          // Getting the device identifier string.
          disconnectionToken.ThrowIfCancellationRequested();
          Identifier = await Device.GetIdentifierAsync();

          // Trying to get the initial getter values of the asynchronous properties.
          // If any getter exception occurs on this stage, throw it and disconnect from the device.
          ThreadExceptionEventHandler throwOnGetterUpdate = (_, args) => throw args.Exception;
          foreach (var (_, asyncProperty) in Device.AsyncProperties)
          {
            disconnectionToken.ThrowIfCancellationRequested();
            asyncProperty.GetterException += throwOnGetterUpdate;
            asyncProperty.RequestGetterUpdate();
            await asyncProperty.GetGetterUpdatingTask();
            asyncProperty.GetterException -= throwOnGetterUpdate;
          }

          // Subscribing on further exception events of the asynchronous properties.
          foreach (var (_, asyncProperty) in Device.AsyncProperties)
          {
            asyncProperty.GetterException += OnException;
            asyncProperty.SetterException += OnException;
          }

          // Configuring the auto-updater.
          disconnectionToken.ThrowIfCancellationRequested();
          AutoUpdater.Delay = TimeSpan.FromMilliseconds(AutoUpdaterDelay);
          if (IsAutoUpdaterEnabled)
            AutoUpdater.Start();

          // Waiting for the disconnection request.
          IsDeviceReady = true;
          ConnectionState = DeviceConnectionState.Connected;
          await Task.Delay(-1, disconnectionToken);

          // Waiting for the auto-updater to stop.
          IsDeviceReady = false;
          ConnectionState = DeviceConnectionState.DeInitializing;
          if (AutoUpdater != null)
            await AutoUpdater.StopAsync();

          // Waiting for the device actions to complete.
          await DeviceActionExecutor.WaitForAllActionsToCompleteAsync();

          // Waiting for all asynchronous properties processing to complete.
          foreach (var asyncPropertyMetadata in AsyncProperties)
          {
            if (asyncPropertyMetadata.AsyncProperty == null)
              continue;

            await asyncPropertyMetadata.AsyncProperty.GetSetterProcessingTask();
            await asyncPropertyMetadata.AsyncProperty.GetGetterUpdatingTask();
          }

          // Waiting for remaining asynchronous operations to complete before session closing.
          await Task.Run(() =>
          {
            lock (DisconnectionLock)
              Device.CloseSession();
          });
          ConnectionState = DeviceConnectionState.Disconnected;
        }
      }
      catch (OperationCanceledException)
      {
        // Suppress task cancellation exceptions.
      }
      catch (Exception e)
      {
        OnException(e);
        ConnectionState = DeviceConnectionState.DisconnectedWithError;
      }
      finally
      {
        DisconnectionTokenSource = null;
        AutoUpdater = null;
        Device = null;

        Identifier = string.Empty;
        RebuildCollections();

        IsDeviceReady = false;
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
      catch (OperationCanceledException)
      {
        // Suppress task cancellation exception.
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
            foreach (var (_, asyncProperty) in Device.AsyncProperties)
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
      OnException(this, new ThreadExceptionEventArgs(exception));

    /// <summary>
    ///   Invokes the <see cref="Exception" /> event.
    /// </summary>
    /// <param name="sender">
    ///   The event sender object.
    /// </param>
    /// <param name="args">
    ///   The event arguments object containing the thrown exception.
    /// </param>
    protected virtual void OnException(object sender, ThreadExceptionEventArgs args) => Exception?.Invoke(sender, args);

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
    ~VisaDeviceController() => Dispose();
  }
}
