using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The class for connectable VISA devices.
  /// </summary>
  public class VisaDevice : IBuildableVisaDevice<IVisaDevice>
  {
    /// <summary>
    ///   Defines the default connection timeout in milliseconds.
    /// </summary>
    public const int DefaultConnectionTimeout = 1000;

    /// <summary>
    ///   Defines the array of all supported hardware interface types.
    /// </summary>
    public static readonly HardwareInterfaceType[] HardwareInterfaceTypes =
      (HardwareInterfaceType[]) Enum.GetValues(typeof(HardwareInterfaceType));

    /// <summary>
    ///   The flag indicating if the VISA device is being disposed of at the moment.
    /// </summary>
    private bool _isDisposing;

    /// <summary>
    ///   The flag indicating if the VISA device has been already disposed of.
    /// </summary>
    private bool _isDisposed;

    /// <summary>
    ///   The flag indicating if this VISA device instance has been cloned from another instance.
    /// </summary>
    private bool _isClone;

    /// <inheritdoc />
    /// <exception cref="VisaDeviceException">
    ///   The resource manager cannot be modified when a session is opened.
    /// </exception>
    public IResourceManager? ResourceManager
    {
      get => _resourceManager;
      set
      {
        if (IsSessionOpened)
          throw new VisaDeviceException(this,
            new InvalidOperationException("The resource manager cannot be modified when a session is opened."));

        _resourceManager = value;
      }
    }
    private IResourceManager? _resourceManager;

    /// <inheritdoc />
    /// <exception cref="VisaDeviceException">
    ///   The resource name cannot be modified when a session is opened.
    /// </exception>
    public string ResourceName
    {
      get => _resourceName;
      set
      {
        if (IsSessionOpened)
          throw new VisaDeviceException(this,
            new InvalidOperationException("The resource name cannot be modified when a session is opened."));

        _resourceName = value;
      }
    }
    private string _resourceName = string.Empty;

    /// <inheritdoc />
    public int ConnectionTimeout
    {
      get => _connectionTimeout;
      set
      {
        if (IsSessionOpened)
          Session!.TimeoutMilliseconds = value;

        _connectionTimeout = value;
      }
    }
    private int _connectionTimeout = DefaultConnectionTimeout;

    /// <inheritdoc />
    public ParseResult? ResourceNameInfo => GetResourceNameInfo();

    /// <inheritdoc />
    public string AliasName => ResourceNameInfo?.AliasIfExists ?? ResourceName;

    /// <summary>
    ///   Gets the array of hardware interface types supported by the device by default.
    ///   It is used when the <see cref="CustomSupportedInterfaces" /> property is set to <c>null</c>.
    ///   Can be overriden in derived classes.
    /// </summary>
    protected virtual HardwareInterfaceType[] DefaultSupportedInterfaces => HardwareInterfaceTypes;

    /// <inheritdoc cref="IBuildableVisaDevice{TVisaDevice}.CustomSupportedInterfaces" />
    protected HardwareInterfaceType[]? CustomSupportedInterfaces { get; set; }

    /// <inheritdoc />
    public HardwareInterfaceType[] SupportedInterfaces => CustomSupportedInterfaces ?? DefaultSupportedInterfaces;

    /// <inheritdoc cref="IBuildableVisaDevice{TVisaDevice}.CustomAsyncProperties" />
    protected ObservableCollection<IAsyncProperty> CustomAsyncProperties { get; } = new();

    /// <summary>
    ///   Gets the enumeration of asynchronous properties declared for the current VISA device instance.
    /// </summary>
    protected IEnumerable<IAsyncProperty> DeclaredAsyncProperties { get; }

    /// <inheritdoc />
    public virtual IEnumerable<IAsyncProperty> AsyncProperties => DeclaredAsyncProperties.Concat(CustomAsyncProperties);

    /// <inheritdoc cref="IBuildableVisaDevice{TVisaDevice}.CustomDeviceActions" />
    protected ObservableCollection<IDeviceAction> CustomDeviceActions { get; } = new();

    /// <summary>
    ///   Gets the enumeration of device actions declared for the current VISA device instance.
    /// </summary>
    protected IEnumerable<IDeviceAction> DeclaredDeviceActions { get; }

    /// <inheritdoc />
    public virtual IEnumerable<IDeviceAction> DeviceActions => DeclaredDeviceActions.Concat(CustomDeviceActions);

    /// <inheritdoc cref="IBuildableVisaDevice{TVisaDevice}.CustomInitializeCallback" />
    protected Action<IVisaDevice>? CustomInitializeCallback { get; set; }

    /// <inheritdoc cref="IBuildableVisaDevice{TVisaDevice}.CustomDeInitializeCallback" />
    protected Action<IVisaDevice>? CustomDeInitializeCallback { get; set; }

    /// <inheritdoc cref="IBuildableVisaDevice{TVisaDevice}.CustomGetIdentifierCallback" />
    protected Func<IVisaDevice, string>? CustomGetIdentifierCallback { get; set; }

    /// <inheritdoc cref="IBuildableVisaDevice{TVisaDevice}.CustomResetCallback" />
    protected Action<IVisaDevice>? CustomResetCallback { get; set; }

    /// <inheritdoc />
    ObservableCollection<IAsyncProperty> IBuildableVisaDevice<IVisaDevice>.CustomAsyncProperties =>
      CustomAsyncProperties;

    /// <inheritdoc />
    ObservableCollection<IDeviceAction> IBuildableVisaDevice<IVisaDevice>.CustomDeviceActions =>
      CustomDeviceActions;

    /// <inheritdoc />
    HardwareInterfaceType[]? IBuildableVisaDevice<IVisaDevice>.CustomSupportedInterfaces
    {
      get => CustomSupportedInterfaces;
      set => CustomSupportedInterfaces = value;
    }

    /// <inheritdoc />
    Action<IVisaDevice>? IBuildableVisaDevice<IVisaDevice>.CustomInitializeCallback
    {
      get => CustomInitializeCallback;
      set => CustomInitializeCallback = value;
    }

    /// <inheritdoc />
    Action<IVisaDevice>? IBuildableVisaDevice<IVisaDevice>.CustomDeInitializeCallback
    {
      get => CustomDeInitializeCallback;
      set => CustomDeInitializeCallback = value;
    }

    /// <inheritdoc />
    Func<IVisaDevice, string>? IBuildableVisaDevice<IVisaDevice>.CustomGetIdentifierCallback
    {
      get => CustomGetIdentifierCallback;
      set => CustomGetIdentifierCallback = value;
    }

    /// <inheritdoc />
    Action<IVisaDevice>? IBuildableVisaDevice<IVisaDevice>.CustomResetCallback
    {
      get => CustomResetCallback;
      set => CustomResetCallback = value;
    }

    /// <inheritdoc />
    public DeviceConnectionState ConnectionState
    {
      get => _connectionState;
      private set
      {
        _connectionState = value;
        OnConnectionStateChanged(value);
      }
    }
    private DeviceConnectionState _connectionState = DeviceConnectionState.Disconnected;

    /// <inheritdoc />
    public IVisaSession? Session { get; private set; }

    /// <inheritdoc />
    public bool IsSessionOpened => Session != null;

    /// <summary>
    ///   Gets the shared locking object used for asynchronous device access synchronization.
    ///   It must be used with <c>lock</c> blocks containing any code where the current opened VISA session can be
    ///   accessed for atomic operation.
    /// </summary>
    protected object SessionLock { get; } = new();

    /// <summary>
    ///   Gets the standard "Reset" device action that calls the <see cref="Reset" /> method.
    /// </summary>
    public IDeviceAction ResetAction => _resetAction ??= new DeviceAction(visaDevice => visaDevice?.Reset())
    {
      Name = nameof(Reset),
      TargetDevice = this
    };
    private IDeviceAction? _resetAction;

    /// <inheritdoc />
    public event EventHandler<DeviceConnectionState>? ConnectionStateChanged;

    /// <summary>
    ///   Creates a new VISA device instance.
    /// </summary>
    public VisaDevice()
    {
      // Collecting the asynchronous properties and device actions declared for the current VISA device instance.
      DeclaredAsyncProperties = CollectDeclaredAsyncProperties();
      DeclaredDeviceActions = CollectDeclaredDeviceActions();

      // Set targets of asynchronous properties and device actions to this device instance when adding them to the
      // corresponding custom collections.
      CustomAsyncProperties.CollectionChanged += OnCustomAsyncPropertiesChanged;
      CustomDeviceActions.CollectionChanged += OnCustomDeviceActionsChanged;
    }

    /// <summary>
    ///   The event handler method that is called when the <see cref="CustomAsyncProperties" /> collection is changed.
    /// </summary>
    private void OnCustomAsyncPropertiesChanged(object? sender, NotifyCollectionChangedEventArgs args) => args.NewItems
      ?.Cast<IAsyncProperty>()
      .ToList()
      .ForEach(asyncProperty => asyncProperty.TargetDevice = this);

    /// <summary>
    ///   The event handler method that is called when the <see cref="CustomDeviceActions" /> collection is changed.
    /// </summary>
    private void OnCustomDeviceActionsChanged(object? sender, NotifyCollectionChangedEventArgs args) => args.NewItems
      ?.Cast<IDeviceAction>()
      .ToList()
      .ForEach(deviceAction => deviceAction.TargetDevice = this);

    /// <inheritdoc cref="IVisaDevice.ResourceNameInfo" />
    private ParseResult? GetResourceNameInfo()
    {
      try
      {
        return ResourceManager != null
          ? ResourceManager.Parse(ResourceName)
          : GlobalResourceManager.Parse(ResourceName);
      }
      catch
      {
        return null;
      }
    }

    /// <summary>
    ///   Collects all asynchronous properties declared for the current VISA device instance.
    ///   Asynchronous properties to be collected must be declared as public properties of types implementing the
    ///   <see cref="IAsyncProperty" /> interface.
    /// </summary>
    /// <returns>
    ///   An enumeration of collected asynchronous properties.
    /// </returns>
    private IEnumerable<IAsyncProperty> CollectDeclaredAsyncProperties() => GetType()
      .GetProperties()
      .Where(property => typeof(IAsyncProperty).IsAssignableFrom(property.PropertyType) && property.CanRead)
      .Select(property =>
      {
        var result = (IAsyncProperty) property.GetValue(this)!;
        result.Name = !string.IsNullOrWhiteSpace(result.Name) ? result.Name : property.Name;
        return result;
      });

    /// <summary>
    ///   Collects all device actions declared for the current VISA device instance.
    ///   Device actions to be collected must be declared as public properties of types implementing the
    ///   <see cref="IDeviceAction" /> interface.
    /// </summary>
    /// <returns>
    ///   An enumeration the collected device actions.
    /// </returns>
    private IEnumerable<IDeviceAction> CollectDeclaredDeviceActions() => GetType()
      .GetProperties()
      .Where(property => typeof(IDeviceAction).IsAssignableFrom(property.PropertyType) && property.CanRead)
      .Select(property =>
      {
        var result = (IDeviceAction) property.GetValue(this)!;
        result.Name = !string.IsNullOrWhiteSpace(result.Name) ? result.Name : property.Name;
        return result;
      });

    /// <summary>
    ///   Throws an <see cref="ObjectDisposedException" /> exception when this VISA device instance has been already
    ///   disposed of. Must be used in any public methods that use device's disposable resources, like
    ///   <see cref="OpenSession" /> and <see cref="CloseSession" />.
    /// </summary>
    protected void ThrowWhenDeviceIsDisposed()
    {
      if (_isDisposed)
        throw new ObjectDisposedException(
          $"The device instance of type \"{GetType().Name}\" has been already disposed of.");
    }

    /// <summary>
    ///   Throws a <see cref="VisaDeviceException" /> exception when no VISA session is opened.
    ///   Must be used in any public methods that require an opened VISA session for functioning, like
    ///   <see cref="Reset" /> and <see cref="GetIdentifier" />.
    ///   This method also logically implicates the <see cref="ThrowWhenDeviceIsDisposed" /> method as no VISA session
    ///   can be opened when a device is disposed of.
    /// </summary>
    protected void ThrowWhenNoVisaSessionIsOpened()
    {
      if (Session == null)
        throw new VisaDeviceException(this,
          new InvalidOperationException("There is no opened VISA session to perform an operation."));
    }

    /// <inheritdoc />
    public void OpenSession()
    {
      ThrowWhenDeviceIsDisposed();

      if (ConnectionState != DeviceConnectionState.Disconnected &&
        ConnectionState != DeviceConnectionState.DisconnectedWithError)
        return;

      try
      {
        ConnectionState = DeviceConnectionState.Initializing;

        if (ResourceNameInfo == null)
          throw new VisaDeviceException(this, new InvalidOperationException(
            $"Cannot parse the resource name \"{ResourceName}\" using the resource manager \"{ResourceManager?.GetType().Name ?? nameof(GlobalResourceManager)}\"."));

        if (!SupportedInterfaces.Contains(ResourceNameInfo.InterfaceType))
          throw new VisaDeviceException(this, new NotSupportedException(
            $"The interface \"{ResourceNameInfo.InterfaceType}\" is not supported by devices of type \"{GetType().Name}\"."));

        Session = ResourceManager != null
          ? ResourceManager.Open(ResourceName, AccessModes.ExclusiveLock, ConnectionTimeout)
          : GlobalResourceManager.Open(ResourceName, AccessModes.ExclusiveLock, ConnectionTimeout);

        lock (SessionLock)
        {
          if (CustomInitializeCallback != null)
            CustomInitializeCallback.Invoke(this);
          else
            DefaultInitializeCallback();
        }

        ConnectionState = DeviceConnectionState.Connected;
      }
      catch (Exception e)
      {
        CloseSession();
        ConnectionState = DeviceConnectionState.DisconnectedWithError;

        throw e is VisaDeviceException visaDeviceException
          ? visaDeviceException
          : new VisaDeviceException(this, e);
      }
    }

    /// <inheritdoc />
    public Task OpenSessionAsync() => Task.Run(OpenSession);

    /// <summary>
    ///   Defines the default device initialization callback method.
    ///   It is called when the <see cref="CustomInitializeCallback" /> property is set to <c>null</c>.
    ///   Can be overriden in derived classes.
    /// </summary>
    protected virtual void DefaultInitializeCallback()
    {
      // Does nothing by default.
    }

    /// <inheritdoc />
    public string GetIdentifier()
    {
      ThrowWhenNoVisaSessionIsOpened();

      lock (SessionLock)
        return CustomGetIdentifierCallback != null
          ? CustomGetIdentifierCallback.Invoke(this)
          : DefaultGetIdentifierCallback();
    }

    /// <inheritdoc />
    public Task<string> GetIdentifierAsync() => Task.Run(GetIdentifier);

    /// <summary>
    ///   Defines the default callback method that gets the device's identifier string.
    ///   It is called when the <see cref="CustomGetIdentifierCallback" /> property is set to <c>null</c>.
    ///   Can be overriden in derived classes.
    /// </summary>
    /// <returns>
    ///   The device's identifier string.
    /// </returns>
    protected virtual string DefaultGetIdentifierCallback() => AliasName;

    /// <inheritdoc />
    public void Reset()
    {
      ThrowWhenNoVisaSessionIsOpened();

      lock (SessionLock)
      {
        if (CustomResetCallback != null)
          CustomResetCallback.Invoke(this);
        else
          DefaultResetCallback();
      }
    }

    /// <inheritdoc />
    public Task ResetAsync() => Task.Run(Reset);

    /// <summary>
    ///   Defines the default device reset callback method.
    ///   It is called when the <see cref="CustomResetCallback" /> property is set to <c>null</c>.
    ///   Can be overriden in derived classes.
    /// </summary>
    protected virtual void DefaultResetCallback()
    {
      // Does nothing by default.
    }

    /// <inheritdoc />
    public void CloseSession()
    {
      if (ConnectionState != DeviceConnectionState.Connected)
        return;

      try
      {
        ConnectionState = DeviceConnectionState.DeInitializing;

        lock (SessionLock)
        {
          if (CustomDeInitializeCallback != null)
            CustomDeInitializeCallback.Invoke(this);
          else
            DefaultDeInitializeCallback();
        }

        Session?.Dispose();
      }
      catch
      {
        // Suppress all exceptions.
      }
      finally
      {
        Session = null;
        ConnectionState = DeviceConnectionState.Disconnected;
      }
    }

    /// <inheritdoc />
    public Task CloseSessionAsync() => Task.Run(CloseSession);

    /// <summary>
    ///   Defines the default device de-initialization callback method.
    ///   It is called when the <see cref="CustomDeInitializeCallback" /> property is set to <c>null</c>.
    ///   Can be overriden in derived classes.
    /// </summary>
    protected virtual void DefaultDeInitializeCallback()
    {
      // Does nothing by default.
    }

    /// <summary>
    ///   Invokes the <see cref="ConnectionStateChanged" /> event.
    /// </summary>
    /// <param name="state">
    ///   The new device connection state.
    /// </param>
    protected virtual void OnConnectionStateChanged(DeviceConnectionState state) =>
      ConnectionStateChanged?.Invoke(this, state);

    /// <inheritdoc />
    public virtual object Clone()
    {
      var clone = (VisaDevice) Activator.CreateInstance(GetType())!;
      clone._isClone = true;

      clone.ResourceManager = ResourceManager != null
        ? (IResourceManager) Activator.CreateInstance(ResourceManager.GetType())!
        : null;
      clone.ResourceName = ResourceName;
      clone.ConnectionTimeout = ConnectionTimeout;

      clone.CustomSupportedInterfaces = CustomSupportedInterfaces;
      CustomAsyncProperties
        .Select(asyncProperty => (IAsyncProperty) asyncProperty.Clone())
        .ToList()
        .ForEach(asyncPropertyClone => clone.CustomAsyncProperties.Add(asyncPropertyClone));
      CustomDeviceActions
        .Select(deviceAction => (IDeviceAction) deviceAction.Clone())
        .ToList()
        .ForEach(deviceActionClone => clone.CustomDeviceActions.Add(deviceActionClone));
      clone.CustomInitializeCallback = CustomInitializeCallback;
      clone.CustomDeInitializeCallback = CustomDeInitializeCallback;
      clone.CustomGetIdentifierCallback = CustomGetIdentifierCallback;
      clone.CustomResetCallback = CustomResetCallback;

      return clone;
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
      if (_isDisposing || _isDisposed)
        return;
      _isDisposing = true;

      CloseSession();

      CustomAsyncProperties.CollectionChanged -= OnCustomAsyncPropertiesChanged;
      CustomDeviceActions.CollectionChanged -= OnCustomDeviceActionsChanged;

      // If this device instance has been previously cloned from another one, dispose of the VISA resource manager
      // object because it was automatically created during the cloning process.
      if (_isClone)
        ResourceManager?.Dispose();

      GC.SuppressFinalize(this);
      _isDisposed = true;
    }

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync() => new(Task.Run(Dispose));

    /// <summary>
    ///   Disposes the object on finalization.
    /// </summary>
    [ExcludeFromCodeCoverage]
    ~VisaDevice() => Dispose();
  }
}
