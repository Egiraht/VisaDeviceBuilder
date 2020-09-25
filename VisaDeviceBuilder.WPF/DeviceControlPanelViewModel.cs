using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ivi.Visa;
using VisaDeviceBuilder.WPF.Components;
using Localization = VisaDeviceBuilder.WPF.Resources.Localization;
using LocalizationResourceManager = System.Resources.ResourceManager;

namespace VisaDeviceBuilder.WPF
{
  /// <summary>
  ///   The view model class for control panels used for manipulating VISA devices through classes implementing
  ///   <see cref="IVisaDevice" /> interface.
  ///   All possible device exceptions can be handled using the <see cref="Exception" /> event.
  /// </summary>
  // TODO: Extract cross-platform logic into the main VisaDeviceBuilder library.
  public partial class DeviceControlPanelViewModel : INotifyPropertyChanged, IDisposable, IAsyncDisposable
  {
    /// <summary>
    ///   The object disposal flag.
    /// </summary>
    private bool _isDisposed = false;

    /// <summary>
    ///   The backing field for the <see cref="DeviceType" /> property.
    /// </summary>
    private Type _deviceType = typeof(VisaDevice);

    /// <summary>
    ///   The backing field for the <see cref="ResourceManagerType" /> property.
    /// </summary>
    private Type? _resourceManagerType;

    /// <summary>
    ///   The backing field for the <see cref="CanConnect" /> property.
    /// </summary>
    private bool _canConnect = true;

    /// <summary>
    ///   The backing field for the <see cref="IsDeviceReady" /> property.
    /// </summary>
    private bool _isDeviceReady = false;

    /// <summary>
    ///   The backing field for the <see cref="ResourceName" /> property.
    /// </summary>
    private string _resourceName = string.Empty;

    /// <summary>
    ///   The backing field for the <see cref="IsUpdatingVisaResources" /> property.
    /// </summary>
    private bool _isUpdatingVisaResources = false;

    /// <summary>
    ///   The backing field for the <see cref="IsUpdatingAsyncProperties" /> property.
    /// </summary>
    private bool _isUpdatingAsyncProperties = false;

    /// <summary>
    ///   The backing field for the <see cref="IsDisconnectionRequested" /> property.
    /// </summary>
    private bool _isDisconnectionRequested = false;

    /// <summary>
    ///   The backing field for the <see cref="DeviceLabel" /> property.
    /// </summary>
    private string _deviceLabel = string.Empty;

    /// <summary>
    ///   The backing field for the <see cref="IsMessageInputPanelEnabled" /> property.
    /// </summary>
    private bool _isMessageInputPanelEnabled;

    /// <summary>
    ///   The backing field for the <see cref="ConnectionState" /> property.
    /// </summary>
    private DeviceConnectionState _connectionState;

    /// <summary>
    ///   The backing field for the <see cref="Identifier" /> property.
    /// </summary>
    private string _identifier = string.Empty;

    /// <summary>
    ///   The backing field for the <see cref="RequestMessage" /> property.
    /// </summary>
    private string _requestMessage = string.Empty;

    /// <summary>
    ///   The backing field for the <see cref="ResponseMessage" /> property.
    /// </summary>
    private string _responseMessage = string.Empty;

    /// <summary>
    ///   The backing field for the <see cref="Device" /> property.
    /// </summary>
    private IVisaDevice? _device;

    /// <summary>
    ///   The backing field for the <see cref="LocalizationResourceManager" /> property.
    /// </summary>
    private LocalizationResourceManager? _localizationResourceManager;

    /// <summary>
    ///   The backing field for the <see cref="AutoUpdater" /> property.
    /// </summary>
    private IAutoUpdater? _autoUpdater;

    /// <summary>
    ///   The backing field for the <see cref="IsAutoUpdaterEnabled" /> property.
    /// </summary>
    private bool _isAutoUpdaterEnabled = true;

    /// <summary>
    ///   The backing field for the <see cref="AutoUpdaterDelay" /> property.
    /// </summary>
    private int _autoUpdaterDelay = 10;

    /// <summary>
    ///   Gets or sets the type of the device.
    ///   The device class defined by the specified type must implement the <see cref="IVisaDevice" /> interface.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///   The provided type value does not implement the <see cref="IVisaDevice" /> interface.
    /// </exception>
    public Type DeviceType
    {
      get => _deviceType;
      set
      {
        if (!value.IsClass || !typeof(IVisaDevice).IsAssignableFrom(value))
        {
          OnException(new InvalidOperationException(string.Format(Localization.InvalidVisaDeviceType, value.Name,
            nameof(IVisaDevice))));
          return;
        }

        _deviceType = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(DeviceLabel));
      }
    }

    /// <summary>
    ///   Gets or sets the type of the VISA resource manager.
    ///   The resource manager class defined by the specified type must implement the <see cref="IResourceManager" />
    ///   interface, or the value can be <c>null</c>.
    ///   If set to <c>null</c>, the default <see cref="GlobalResourceManager" /> static class will be used.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///   The provided type does not implement the <see cref="IResourceManager" /> interface.
    /// </exception>
    public Type? ResourceManagerType
    {
      get => _resourceManagerType;
      set
      {
        if (value != null && (!value.IsClass || !typeof(IResourceManager).IsAssignableFrom(value)))
        {
          OnException(new InvalidOperationException(string.Format(Localization.InvalidResourceManagerType, value.Name,
            nameof(IResourceManager))));
          return;
        }

        _resourceManagerType = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Gets or sets the text label used for device distinguishing among the devices of similar type.
    /// </summary>
    public string DeviceLabel
    {
      get => !string.IsNullOrEmpty(_deviceLabel) ? _deviceLabel : DeviceType.Name;
      set
      {
        _deviceLabel = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Checks or sets the value if the message input panel should be enabled.
    /// </summary>
    public bool IsMessageInputPanelEnabled
    {
      get => _isMessageInputPanelEnabled;
      set
      {
        _isMessageInputPanelEnabled = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Gets or sets the resource name used for VISA device connection.
    /// </summary>
    public string ResourceName
    {
      get => _resourceName;
      set
      {
        _resourceName = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Gets the list of available VISA resource names.
    /// </summary>
    public ObservableCollection<string> AvailableVisaResources { get; } = new ObservableCollection<string>();

    /// <summary>
    ///   Checks if the device can be connected at the moment.
    /// </summary>
    public bool CanConnect
    {
      get => _canConnect;
      protected set
      {
        _canConnect = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Checks if the device is connected, initialized, and ready for communication.
    /// </summary>
    public bool IsDeviceReady
    {
      get => _isDeviceReady;
      protected set
      {
        _isDeviceReady = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Gets the current device connection state.
    /// </summary>
    public DeviceConnectionState ConnectionState
    {
      get => _connectionState;
      protected set
      {
        _connectionState = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Checks if the <see cref="AvailableVisaResources" /> property is being updated.
    /// </summary>
    public bool IsUpdatingVisaResources
    {
      get => _isUpdatingVisaResources;
      protected set
      {
        _isUpdatingVisaResources = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Checks if the asynchronous properties are being updated at the moment after calling the
    ///   <see cref="UpdateAsyncPropertiesAsync"/> method.
    /// </summary>
    public bool IsUpdatingAsyncProperties
    {
      get => _isUpdatingAsyncProperties;
      protected set
      {
        _isUpdatingAsyncProperties = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Checks if the device disconnection has been requested.
    /// </summary>
    public bool IsDisconnectionRequested
    {
      get => _isDisconnectionRequested;
      protected set
      {
        _isDisconnectionRequested = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Gets the <see cref="IVisaDevice" /> instance bound to this control.
    /// </summary>
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

    /// <summary>
    ///   Checks if the <see cref="Device" /> is not <c>null</c> and its type implements <see cref="IMessageDevice" />.
    /// </summary>
    public bool IsMessageDevice => Device is IMessageDevice;

    /// <summary>
    ///   Gets the device identifier.
    /// </summary>
    public string Identifier
    {
      get => _identifier;
      protected set
      {
        _identifier = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Gets the collection of asynchronous properties and corresponding metadata defined for the device.
    /// </summary>
    public ObservableCollection<AsyncPropertyMetadata> AsyncProperties { get; } =
      new ObservableCollection<AsyncPropertyMetadata>();

    /// <summary>
    ///   Gets the collection of device actions and corresponding metadata defined for the device.
    /// </summary>
    public ObservableCollection<DeviceActionMetadata> DeviceActions { get; } =
      new ObservableCollection<DeviceActionMetadata>();

    /// <summary>
    ///   Gets or sets the optional ResX resource manager instance used for localization of the names of available
    ///   asynchronous properties and actions.
    ///   The provided localization resource manager must be able to accept the original names of the asynchronous
    ///   properties and actions and return their localized names.
    ///   If not provided, the original names will be used without localization.
    /// </summary>
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

    /// <summary>
    ///   Checks or sets if the auto-updater for asynchronous properties is enabled.
    /// </summary>
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

    /// <summary>
    ///   Gets or sets the auto-updater cycle delay in milliseconds.
    /// </summary>
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
    protected Task? DeviceConnectionTask { get; set; }

    /// <summary>
    ///   Stores the cancellation token source that allows to stop the device connection task and disconnect the device.
    /// </summary>
    protected CancellationTokenSource? DeviceConnectionStopTokenSource { get; set; }

    /// <summary>
    ///   Gets or sets the command message string to be sent to the device.
    /// </summary>
    public string RequestMessage
    {
      get => _requestMessage;
      set
      {
        _requestMessage = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Gets the command response string received from the device for the last command.
    /// </summary>
    public string ResponseMessage
    {
      get => _responseMessage;
      protected set
      {
        _responseMessage = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Gets the shared locking object used for device disconnection synchronization.
    /// </summary>
    protected object DisconnectionLock { get; } = new object();

    /// <summary>
    ///   Event that is called on any property change.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    ///   Event that is called on any control exception.
    /// </summary>
    public event ThreadExceptionEventHandler? Exception;

    /// <summary>
    ///   Creates a new view-model instance.
    /// </summary>
    public DeviceControlPanelViewModel() => DeviceActionExecutor.Exception += OnException;

    /// <summary>
    ///   Asynchronously updates the list of VISA resource names.
    /// </summary>
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
          using var resourceManager = ResourceManagerType != null
            ? (IResourceManager?) Activator.CreateInstance(ResourceManagerType)
            : null;
          return (resourceManager != null ? resourceManager.Find("?*::INSTR") : GlobalResourceManager.Find("?*::INSTR"))
            .Aggregate(new List<string>(), (results, resource) =>
            {
              var parseResult = resourceManager != null
                ? resourceManager.Parse(resource)
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
        throw new InvalidOperationException(string.Format(Localization.InvalidVisaDeviceType,
          deviceType.Name, nameof(IVisaDevice)));

      return (IVisaDevice) (Activator.CreateInstance(deviceType, resourceName, resourceManager) ??
        throw new NotSupportedException(string.Format(Localization.CannotCreateVisaDeviceInstance, deviceType.Name)));
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

    /// <summary>
    ///   Starts the asynchronous device connection process.
    ///   The created device connection task can be accessed via the <see cref="DeviceConnectionTask" /> property.
    /// </summary>
    public virtual void Connect()
    {
      if (!CanConnect)
        return;

      // Checking the VISA resource name.
      using (var resourceManager = ResourceManagerType != null
        ? (IResourceManager?) Activator.CreateInstance(ResourceManagerType)
        : null)
      {
        try
        {
          if (resourceManager != null)
            resourceManager.Parse(ResourceName);
          else
            GlobalResourceManager.Parse(ResourceName);
        }
        catch
        {
          return;
        }
      }

      DeviceConnectionTask = CreateDeviceConnectionTask();
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
        using var resourceManager = ResourceManagerType != null
          ? (IResourceManager?) Activator.CreateInstance(ResourceManagerType)
          : null;

        await using (Device = CreateDeviceInstance(ResourceName, DeviceType, resourceManager))
        await using (AutoUpdater = new AutoUpdater(Device))
        using (DeviceConnectionStopTokenSource = new CancellationTokenSource())
        {
          var stopToken = DeviceConnectionStopTokenSource.Token;
          CanConnect = false;
          ConnectionState = DeviceConnectionState.Initializing;

          // Trying to connect to the device.
          stopToken.ThrowIfCancellationRequested();
          var sessionOpeningTask = Device.OpenSessionAsync();
          await sessionOpeningTask;

          // Rebuilding and localizing the collections of asynchronous properties and device actions.
          RebuildCollections();

          // Getting the device identifier string.
          stopToken.ThrowIfCancellationRequested();
          Identifier = await Device.GetIdentifierAsync();

          // Trying to get the initial getter values of the asynchronous properties.
          ThreadExceptionEventHandler throwOnGetterUpdate = (_, args) => throw args.Exception;
          foreach (var (_, asyncProperty) in Device.AsyncProperties)
          {
            stopToken.ThrowIfCancellationRequested();
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
          stopToken.ThrowIfCancellationRequested();
          AutoUpdater.Delay = TimeSpan.FromMilliseconds(AutoUpdaterDelay);
          if (IsAutoUpdaterEnabled)
            AutoUpdater.Start();

          // Waiting for the disconnection request.
          IsDeviceReady = true;
          ConnectionState = DeviceConnectionState.Connected;
          await Task.Delay(-1, stopToken);

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
              Device.CloseSessionAsync().Wait();
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
        DeviceConnectionStopTokenSource = null;
        AutoUpdater = null;
        Device = null;

        Identifier = string.Empty;
        RebuildCollections();

        IsDeviceReady = false;
        CanConnect = true;
      }
    }

    /// <summary>
    ///   Stops the device connection loop.
    /// </summary>
    public virtual async Task DisconnectAsync()
    {
      if (IsDisconnectionRequested || DeviceConnectionTask == null || DeviceConnectionStopTokenSource == null)
        return;
      IsDisconnectionRequested = true;

      try
      {
        DeviceConnectionStopTokenSource.Cancel();
        await DeviceConnectionTask;
      }
      catch (OperationCanceledException)
      {
        // Suppress task cancellation exception.
      }
      finally
      {
        DeviceConnectionTask.Dispose();
        DeviceConnectionTask = null;
        IsDisconnectionRequested = false;
      }
    }

    /// <summary>
    ///   Asynchronously updates getters of all asynchronous properties available in the attached <see cref="Device" />
    ///   instance.
    /// </summary>
    public virtual Task UpdateAsyncPropertiesAsync() => Task.Run(() =>
    {
      lock (DisconnectionLock)
      {
        if (Device?.IsSessionOpened != true || IsUpdatingAsyncProperties)
          return;
        IsUpdatingAsyncProperties = true;

        try
        {
          foreach (var (_, property) in Device.AsyncProperties)
          {
            property.RequestGetterUpdate();
            property.GetGetterUpdatingTask().Wait();
          }
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
    });

    /// <summary>
    ///   Asynchronously sends the message to the connected device.
    /// </summary>
    public virtual Task SendMessageAsync() => Task.Run(() =>
    {
      lock (DisconnectionLock)
      {
        try
        {
          if (!IsMessageDevice)
            return;

          ResponseMessage = ((IMessageDevice) Device!).SendMessage(RequestMessage);
        }
        catch (Exception exception)
        {
          OnException(exception);
        }
      }
    });

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
    public virtual void Dispose() => Task.Run(DisposeAsync).Wait();

    /// <inheritdoc />
    ~DeviceControlPanelViewModel() => Dispose();
  }
}
