using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ivi.Visa;
using VisaDeviceBuilder.WPF.Components;
using Localization = VisaDeviceBuilder.WPF.Resources.Localization;

namespace VisaDeviceBuilder.WPF
{
  /// <summary>
  ///   The view model class for control panels used for manipulating VISA devices through classes implementing
  ///   <see cref="IVisaDevice" /> interface.
  ///   All possible device exceptions can be handled using the <see cref="Exception" /> event.
  /// </summary>
  public partial class DeviceControlPanelViewModel : INotifyPropertyChanged, IDisposable
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
    ///   The backing field for the <see cref="ResourceManager" /> property.
    /// </summary>
    private IResourceManager? _resourceManager;

    /// <summary>
    ///   The backing field for the <see cref="ResourceName" /> property.
    /// </summary>
    private string _resourceName = string.Empty;

    /// <summary>
    ///   The backing field for the <see cref="IsUpdatingVisaResources" /> property.
    /// </summary>
    private bool _isUpdatingVisaResources = false;

    /// <summary>
    ///   The backing field for the <see cref="DeviceLabel" /> property.
    /// </summary>
    private string _deviceLabel = string.Empty;

    /// <summary>
    ///   The backing field for the <see cref="IsCommandInputEnabled" /> property.
    /// </summary>
    private bool _isCommandInputEnabled;

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
    /// <exception cref="InvalidCastException">
    ///   The provided type value does not implement the <see cref="IVisaDevice" /> interface.
    /// </exception>
    public Type DeviceType
    {
      get => _deviceType;
      set
      {
        if (!value.IsClass || !typeof(IVisaDevice).IsAssignableFrom(value))
        {
          OnException(new InvalidCastException(string.Format(Localization.InvalidVisaDeviceType, value.Name,
            nameof(IVisaDevice))));
          return;
        }

        _deviceType = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(DeviceLabel));
        CreateNewDeviceInstance();
      }
    }

    /// <summary>
    ///   Gets or sets the type of the VISA resource manager.
    ///   The resource manager class defined by the specified type must implement the <see cref="IResourceManager" />
    ///   interface, or the value can be <c>null</c>.
    ///   If set to <c>null</c>, the default <see cref="GlobalResourceManager" /> static class will be used.
    /// </summary>
    /// <exception cref="InvalidCastException">
    ///   The provided type value does not implement the <see cref="IResourceManager" /> interface.
    /// </exception>
    public Type? ResourceManagerType
    {
      get => _resourceManagerType;
      set
      {
        if (value != null && (!value.IsClass || !typeof(IResourceManager).IsAssignableFrom(value)))
        {
          OnException(new InvalidCastException(string.Format(Localization.InvalidResourceManagerType, value.Name,
            nameof(IResourceManager))));
          return;
        }

        _resourceManagerType = value;
        OnPropertyChanged();
        CreateNewDeviceInstance();
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
    ///   Checks or sets the value if the command input should be enabled.
    /// </summary>
    public bool IsCommandInputEnabled
    {
      get => _isCommandInputEnabled;
      set
      {
        _isCommandInputEnabled = value;
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
        CreateNewDeviceInstance();
      }
    }

    /// <summary>
    ///   Gets the list of available VISA resource names.
    /// </summary>
    public ObservableCollection<string> AvailableVisaResources { get; } = new ObservableCollection<string>();

    /// <summary>
    ///   Gets the attached custom VISA resource manager instance.
    /// </summary>
    public IResourceManager? ResourceManager
    {
      get => _resourceManager;
      private set
      {
        _resourceManager = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Checks if the device can be connected at the moment.
    /// </summary>
    public bool CanConnect
    {
      get
      {
        try
        {
          if (ResourceManager != null)
            ResourceManager.Parse(ResourceName);
          else
            GlobalResourceManager.Parse(ResourceName);

          return ConnectionState == DeviceConnectionState.Disconnected ||
            ConnectionState == DeviceConnectionState.DisconnectedWithError;
        }
        catch
        {
          return false;
        }
      }
    }

    /// <summary>
    ///   Checks if the device is successfully connected.
    /// </summary>
    public bool IsConnected => Device?.DeviceConnectionState == DeviceConnectionState.Connected;

    /// <summary>
    ///   Gets the current device connection state.
    /// </summary>
    public DeviceConnectionState ConnectionState => Device?.DeviceConnectionState ?? DeviceConnectionState.Disconnected;

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
    ///   Gets the <see cref="IVisaDevice" /> instance bound to this control.
    /// </summary>
    public IVisaDevice? Device
    {
      get => _device;
      set
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
      private set
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
    ///   Gets the collection of asynchronous actions and corresponding metadata defined for the device.
    /// </summary>
    public ObservableCollection<AsyncActionMetadata> AsyncActions { get; } =
      new ObservableCollection<AsyncActionMetadata>();

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

        if (!IsConnected || AutoUpdater == null)
          return;

        if (_isAutoUpdaterEnabled)
          AutoUpdater.Start();
        else
          AutoUpdater.Stop();
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

        if (AutoUpdater == null)
          return;

        AutoUpdater.Delay = TimeSpan.FromMilliseconds(_autoUpdaterDelay);
      }
    }

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
      private set
      {
        _responseMessage = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Event that is called on any property change.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    ///   Event that is called on any control exception.
    /// </summary>
    public event ThreadExceptionEventHandler? Exception;

    /// <summary>
    ///   Creates a new VISA device instance attached to this control.
    /// </summary>
    private void CreateNewDeviceInstance()
    {
      try
      {
        Device?.Dispose();
        ResourceManager?.Dispose();

        ResourceManager = ResourceManagerType != null
          ? (IResourceManager?) Activator.CreateInstance(ResourceManagerType)
          : null;

        Device = CanConnect
          ? (IVisaDevice?) Activator.CreateInstance(DeviceType, ResourceName, ResourceManager)
          : null;
        OnPropertyChanged(nameof(CanConnect));
        OnPropertyChanged(nameof(IsConnected));

        AsyncProperties.Clear();
        if (Device != null)
          foreach (var (name, asyncProperty) in Device.AsyncProperties)
            AsyncProperties.Add(new AsyncPropertyMetadata
            {
              Name = name,
              AsyncProperty = asyncProperty
            });
        OnPropertyChanged(nameof(AsyncProperties));

        AsyncActions.Clear();
        if (Device != null)
          foreach (var (name, asyncAction) in Device.AsyncActions)
            AsyncActions.Add(new AsyncActionMetadata
            {
              Name = name,
              AsyncAction = asyncAction
            });
        OnPropertyChanged(nameof(AsyncActions));

        AutoUpdater?.Dispose();
        AutoUpdater = Device != null ? new AutoUpdater(Device) : null;
        if (AutoUpdater != null)
          AutoUpdater.AutoUpdateException += Exception;
      }
      catch (Exception e)
      {
        OnException(e);
      }
    }

    /// <summary>
    ///   Asynchronously updates the list of VISA resource names.
    /// </summary>
    public async Task UpdateResourcesListAsync()
    {
      try
      {
        if (IsUpdatingVisaResources)
          return;

        IsUpdatingVisaResources = true;

        var resources = await Task.Run(() => ResourceManager != null
          ? ResourceManager.Find("?*::INSTR")
          : GlobalResourceManager.Find("?*::INSTR"));

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
    ///   Asynchronously connects to the device.
    /// </summary>
    public async Task ConnectAsync()
    {
      try
      {
        if (!CanConnect)
          return;

        var sessionTask = Device!.OpenSessionAsync();
        OnPropertyChanged(nameof(ConnectionState));
        OnPropertyChanged(nameof(CanConnect));
        OnPropertyChanged(nameof(IsConnected));

        await sessionTask;
        await Device.ResetAsync();
        Identifier = await Device.GetIdentifierAsync();
        await UpdateAsyncPropertiesAsync();

        if (AutoUpdater == null)
          return;

        AutoUpdater.Delay = TimeSpan.FromMilliseconds(_autoUpdaterDelay);
        if (IsAutoUpdaterEnabled)
          AutoUpdater.Start();
      }
      catch (Exception e)
      {
        OnException(e);
      }
      finally
      {
        OnPropertyChanged(nameof(ConnectionState));
        OnPropertyChanged(nameof(CanConnect));
        OnPropertyChanged(nameof(IsConnected));
      }
    }

    /// <summary>
    ///   Asynchronously disconnects from the device.
    /// </summary>
    public async Task DisconnectAsync()
    {
      try
      {
        if (!IsConnected)
          return;

        AutoUpdater?.Stop();

        var sessionTask = Device!.CloseSessionAsync();
        OnPropertyChanged(nameof(ConnectionState));
        OnPropertyChanged(nameof(CanConnect));
        OnPropertyChanged(nameof(IsConnected));

        await sessionTask;
      }
      catch (Exception e)
      {
        OnException(e);
      }
      finally
      {
        OnPropertyChanged(nameof(ConnectionState));
        OnPropertyChanged(nameof(CanConnect));
        OnPropertyChanged(nameof(IsConnected));
      }
    }

    /// <summary>
    ///   Asynchronously updates getters of all asynchronous properties available in the attached <see cref="Device" />
    ///   instance.
    /// </summary>
    public async Task UpdateAsyncPropertiesAsync()
    {
      try
      {
        if (Device?.IsSessionOpened != true)
          return;

        foreach (var (_, property) in Device.AsyncProperties)
          await property.UpdateGetterAsync();
      }
      catch (Exception e)
      {
        OnException(e);
      }
    }

    /// <summary>
    ///   Asynchronously sends the message to the connected device.
    /// </summary>
    public async Task SendMessageAsync()
    {
      try
      {
        if (!IsMessageDevice)
          return;

        ResponseMessage = await ((IMessageDevice) Device!).SendMessageAsync(RequestMessage) ?? "";
      }
      catch (Exception exception)
      {
        OnException(exception);
      }
    }

    /// <summary>
    ///   Invokes the <see cref="PropertyChanged" /> event.
    /// </summary>
    /// <param name="propertyName">
    ///   Name of the property being changed.
    ///   If set to <c>null</c> the caller member name is used.
    /// </param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    ///   Invokes the <see cref="Exception" /> event.
    /// </summary>
    /// <param name="exception">
    ///   The exception instance to be provided with the event.
    /// </param>
    private void OnException(Exception exception) =>
      Exception?.Invoke(this, new ThreadExceptionEventArgs(exception));

    /// <inheritdoc />
    public void Dispose()
    {
      if (_isDisposed)
        return;

      try
      {
        AutoUpdater?.Dispose();
        Device?.Dispose();
        ResourceManager?.Dispose();
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
    ~DeviceControlPanelViewModel()
    {
      Dispose();
    }
  }
}
