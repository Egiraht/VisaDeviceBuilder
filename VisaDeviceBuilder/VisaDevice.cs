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
    ///   The flag indicating if the object has been already disposed.
    /// </summary>
    private bool _isDisposed;

    /// <inheritdoc />
    public IResourceManager? ResourceManager
    {
      get => _resourceManager;
      set
      {
        if (IsSessionOpened)
          throw new VisaDeviceException(this, "The resource manager cannot be modified when a session is opened.");

        _resourceManager = value;
      }
    }
    private IResourceManager? _resourceManager;

    /// <inheritdoc />
    public string ResourceName
    {
      get => _resourceName;
      set
      {
        if (IsSessionOpened)
          throw new VisaDeviceException(this, "The resource name cannot be modified when a session is opened.");

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
    ///   Collects all asynchronous properties defined in the current device class into a dictionary.
    ///   Asynchronous properties to be collected must be declared as public properties of types implementing the
    ///   <see cref="IAsyncProperty" /> interface.
    /// </summary>
    /// <returns>
    ///   The dictionary with the collected device actions.
    ///   Keys of the dictionary contain the declaring member names while the corresponding device action instances are
    ///   stored as values.
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
    ///   Collects all device actions defined in the current device class into a dictionary.
    ///   Device actions to be collected must be declared as public properties of types implementing the
    ///   <see cref="IDeviceAction" /> interface, or as public methods having the <see cref="Action" /> delegate
    ///   signature and decorated with the <see cref="DeviceActionAttribute" />.
    /// </summary>
    /// <returns>
    ///   The dictionary with the collected device actions.
    ///   Keys of the dictionary contain the declaring member names while the corresponding device action instances are
    ///   stored as values.
    /// </returns>
    private IEnumerable<IDeviceAction> CollectOwnDeviceActions()
    {
      return GetType()
        .GetProperties()
        .Where(property => typeof(IDeviceAction).IsAssignableFrom(property.PropertyType) && property.CanRead)
        .Select(property =>
        {
          var result = (IDeviceAction) property.GetValue(this)!;
          result.Name = !string.IsNullOrWhiteSpace(result.Name) ? result.Name : property.Name;
          return result;
        })
        .Concat(GetType()
          .GetMethods()
          .Where(method => Utilities.ValidateDelegate<Action>(method) &&
            method.GetCustomAttribute<DeviceActionAttribute>() != null)
          .Select(method =>
          {
            var attribute = method.GetCustomAttribute<DeviceActionAttribute>();
            return (IDeviceAction) new DeviceAction((Action) method.CreateDelegate(typeof(Action), this))
            {
              Name = !string.IsNullOrWhiteSpace(attribute?.Name) ? attribute.Name : method.Name,
            };
          }));
    }

    /// <inheritdoc />
    public void OpenSession()
    {
      if (_isDisposed)
        throw new ObjectDisposedException(GetType().Name);

      if (ConnectionState != DeviceConnectionState.Disconnected &&
        ConnectionState != DeviceConnectionState.DisconnectedWithError)
        return;

      try
      {
        ConnectionState = DeviceConnectionState.Initializing;

        if (ResourceNameInfo == null)
          throw new VisaDeviceException(this, new NotSupportedException(
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
    public Task OpenSessionAsync() => Task.Run(OpenSession);

    /// <summary>
    ///   Initializes the device after the successful session opening.
    /// </summary>
    protected virtual void Initialize()
    {
      lock (SessionLock)
      {
      }
    }

    /// <inheritdoc />
    public virtual string GetIdentifier()
    {
      lock (SessionLock)
        return AliasName;
    }

    /// <inheritdoc />
    public virtual Task<string> GetIdentifierAsync() => Task.Run(GetIdentifier);

    /// <inheritdoc />
    [DeviceAction]
    public virtual void Reset()
    {
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
      lock (SessionLock)
      {
      }
    }

    /// <inheritdoc />
    public void CloseSession()
    {
      if (_isDisposed)
        throw new ObjectDisposedException(GetType().Name);

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
      if (_isDisposed)
        return;

      CloseSession();

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
