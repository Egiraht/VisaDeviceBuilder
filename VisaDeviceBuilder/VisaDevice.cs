using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The class for connectable VISA devices.
  /// </summary>
  public class VisaDevice : IVisaDevice
  {
    /// <summary>
    ///   Defines the default connection timeout in milliseconds.
    /// </summary>
    public const int DefaultConnectionTimeout = 1000;

    /// <summary>
    ///   Defines the default collection of supported hardware interface types.
    /// </summary>
    public static readonly HardwareInterfaceType[] DefaultSupportedInterfaces =
      (HardwareInterfaceType[]) Enum.GetValues(typeof(HardwareInterfaceType));

    /// <summary>
    ///   Gets or sets the flag indicating if the VISA device is being disposed of at the moment.
    /// </summary>
    protected bool IsDisposing { get; set; }

    /// <summary>
    ///   Gets or sets the flag indicating if the VISA device has been already disposed of.
    /// </summary>
    protected bool IsDisposed { get; set; }

    /// <summary>
    ///   Gets or sets the flag indicating if this VISA device instance has been cloned from another instance.
    /// </summary>
    protected bool IsClone { get; set; }

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

    /// <inheritdoc />
    public virtual HardwareInterfaceType[] SupportedInterfaces => DefaultSupportedInterfaces;

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

    /// <inheritdoc />
    public virtual IEnumerable<IAsyncProperty> AsyncProperties { get; }

    /// <inheritdoc />
    public virtual IEnumerable<IDeviceAction> DeviceActions { get; }

    /// <inheritdoc />
    public event EventHandler<DeviceConnectionState>? ConnectionStateChanged;

    /// <summary>
    ///   Creates a new VISA device instance.
    /// </summary>
    public VisaDevice()
    {
      AsyncProperties = CollectOwnAsyncProperties();
      DeviceActions = CollectOwnDeviceActions();
    }

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
    ///   Enumerates all asynchronous properties defined in the current device class.
    ///   Asynchronous properties to be collected must be declared as public properties of types implementing the
    ///   <see cref="IAsyncProperty" /> interface.
    /// </summary>
    /// <returns>
    ///   An enumeration of collected device actions.
    /// </returns>
    private IEnumerable<IAsyncProperty> CollectOwnAsyncProperties() => GetType()
      .GetProperties()
      .Where(property => typeof(IAsyncProperty).IsAssignableFrom(property.PropertyType) && property.CanRead)
      .Select(property =>
      {
        var result = (IAsyncProperty) property.GetValue(this)!;
        result.Name = !string.IsNullOrWhiteSpace(result.Name) ? result.Name : property.Name;
        return result;
      });

    /// <summary>
    ///   Enumerates all device actions defined in the current device class.
    ///   Device actions to be collected must be declared as public properties of types implementing the
    ///   <see cref="IDeviceAction" /> interface, or as public methods having the <see cref="Action" /> delegate
    ///   signature and decorated with the <see cref="DeviceActionAttribute" />.
    /// </summary>
    /// <returns>
    ///   An enumeration the collected device actions.
    /// </returns>
    private IEnumerable<IDeviceAction> CollectOwnDeviceActions()
    {
      var declaredActions = GetType()
        .GetProperties()
        .Where(property => typeof(IDeviceAction).IsAssignableFrom(property.PropertyType) && property.CanRead)
        .Select(property =>
        {
          var result = (IDeviceAction) property.GetValue(this)!;
          result.Name = !string.IsNullOrWhiteSpace(result.Name) ? result.Name : property.Name;
          return result;
        });

      var decoratedActions = GetType()
        .GetMethods()
        .Where(method => method.ValidateDelegateType<Action>() &&
          method.GetCustomAttribute<DeviceActionAttribute>() != null)
        .Select(method =>
        {
          var attribute = method.GetCustomAttribute<DeviceActionAttribute>();
          return (IDeviceAction) new DeviceAction((Action) method.CreateDelegate(typeof(Action), this))
          {
            Name = !string.IsNullOrWhiteSpace(attribute?.Name) ? attribute.Name : method.Name,
          };
        });

      return declaredActions.Concat(decoratedActions);
    }

    /// <summary>
    ///   Throws an <see cref="ObjectDisposedException" /> exception when this VISA device instance has been already
    ///   disposed of. Must be used in any public methods that use device's disposable resources, like
    ///   <see cref="OpenSession" /> and <see cref="CloseSession" />.
    /// </summary>
    protected void ThrowWhenDeviceIsDisposed()
    {
      if (IsDisposed)
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
    /// <exception cref="VisaDeviceException">
    ///   The used resource manager cannot parse the provided resource name (inner
    ///   <see cref="InvalidOperationException" />), or the used hardware interface is not supported by VISA devices of
    ///   this type (inner <see cref="NotSupportedException" />), or any other device-specific error identified through
    ///   the inner exception.
    /// </exception>
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
          Initialize();

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
    /// <exception cref="VisaDeviceException">
    ///   The used resource manager cannot parse the provided resource name (inner
    ///   <see cref="InvalidOperationException" />), or the used hardware interface is not supported by VISA devices of
    ///   this type (inner <see cref="NotSupportedException" />), or any other device-specific error identified through
    ///   the inner exception.
    /// </exception>
    public Task OpenSessionAsync() => Task.Run(OpenSession);

    /// <summary>
    ///   Initializes the device after the successful session opening.
    /// </summary>
    protected virtual void Initialize()
    {
      // Added as a notification that a session lock should be used when accessing a VISA session in overriding methods.
      lock (SessionLock)
      {
      }
    }

    /// <inheritdoc />
    public virtual string GetIdentifier()
    {
      // Added because this method intrinsically requires an opened session and should also be called when overriding.
      ThrowWhenNoVisaSessionIsOpened();

      // Added as a notification that a session lock should be used when accessing a VISA session in overriding methods.
      lock (SessionLock)
        return AliasName;
    }

    /// <inheritdoc />
    public virtual Task<string> GetIdentifierAsync() => Task.Run(GetIdentifier);

    /// <inheritdoc />
    [DeviceAction]
    public virtual void Reset()
    {
      // Added because this method intrinsically requires an opened session and should also be called when overriding.
      ThrowWhenNoVisaSessionIsOpened();

      // Added as a notification that a session lock should be used when accessing a VISA session in overriding methods.
      lock (SessionLock)
      {
      }
    }

    /// <inheritdoc />
    public virtual Task ResetAsync() => Task.Run(Reset);

    /// <summary>
    ///   De-initializes the device before the session closing.
    /// </summary>
    protected virtual void DeInitialize()
    {
      // Added as a notification that a session lock should be used when accessing a VISA session in overriding methods.
      lock (SessionLock)
      {
      }
    }

    /// <inheritdoc />
    public void CloseSession()
    {
      ThrowWhenDeviceIsDisposed();

      if (ConnectionState != DeviceConnectionState.Connected)
        return;

      try
      {
        ConnectionState = DeviceConnectionState.DeInitializing;

        lock (SessionLock)
          DeInitialize();

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
      var device = (VisaDevice) Activator.CreateInstance(GetType())!;
      device.IsClone = true;
      device.ResourceManager = ResourceManager != null
        ? (IResourceManager) Activator.CreateInstance(ResourceManager.GetType())!
        : null;
      device.ResourceName = ResourceName;
      device.ConnectionTimeout = ConnectionTimeout;
      return device;
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
      if (IsDisposing || IsDisposed)
        return;
      IsDisposing = true;

      CloseSession();

      // If the device has been cloned, dispose of the VISA resource manager object created during the cloning process.
      if (IsClone)
        ResourceManager?.Dispose();

      GC.SuppressFinalize(this);
      IsDisposed = true;
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
