using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ivi.Visa;
using VisaDeviceBuilder.Exceptions;

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
    public const int DefaultConnectionTimeout = 3000;

    /// <summary>
    ///   The backing field for the <see cref="AliasName" /> property.
    /// </summary>
    private string _aliasName = "";

    /// <summary>
    ///   The flag indicating if the object has been already disposed.
    /// </summary>
    private bool _isDisposed = false;

    /// <summary>
    ///   Defines the default collection of supported hardware interface types.
    /// </summary>
    private static readonly HardwareInterfaceType[] DefaultSupportedInterfaces =
      (HardwareInterfaceType[]) Enum.GetValues(typeof(HardwareInterfaceType));

    /// <inheritdoc />
    public string ResourceName { get; }

    /// <inheritdoc />
    public int ConnectionTimeout { get; }

    /// <inheritdoc />
    public string AliasName
    {
      get => !string.IsNullOrEmpty(_aliasName) ? _aliasName : ResourceName;
      protected set => _aliasName = value;
    }

    /// <inheritdoc />
    public HardwareInterfaceType Interface { get; }

    /// <inheritdoc />
    public virtual HardwareInterfaceType[] SupportedInterfaces => DefaultSupportedInterfaces;

    /// <inheritdoc />
    public DeviceConnectionState DeviceConnectionState { get; protected set; } = DeviceConnectionState.Disconnected;

    /// <inheritdoc />
    public IVisaSession? Session { get; protected set; }

    /// <inheritdoc />
    public virtual bool IsSessionOpened => Session != null;

    /// <inheritdoc />
    public ICollection<IRemoteProperty> RemoteProperties { get; }

    /// <summary>
    ///   Creates a new instance of a custom VISA device.
    /// </summary>
    /// <param name="resourceName">
    ///   The VISA resource name of the device.
    /// </param>
    /// <param name="connectionTimeout">
    ///   The connection timeout in milliseconds.
    ///   Defaults to the <see cref="DefaultConnectionTimeout" /> value.
    /// </param>
    public VisaDevice(string resourceName, int connectionTimeout = DefaultConnectionTimeout)
    {
      var parsedResourceName = GlobalResourceManager.Parse(resourceName);
      ResourceName = parsedResourceName.ExpandedUnaliasedName;
      AliasName = parsedResourceName.AliasIfExists;
      Interface = parsedResourceName.InterfaceType;
      ConnectionTimeout = connectionTimeout;
      AsyncProperties = GetType()
        .GetProperties()
        .Where(property => typeof(IRemoteProperty).IsAssignableFrom(property.PropertyType) && property.CanRead)
        .Select(property => (IRemoteProperty) property.GetValue(this))
        .ToList();
    }

    /// <inheritdoc />
    public virtual async Task OpenSessionAsync()
    {
      if (!SupportedInterfaces.Contains(Interface))
        throw new VisaDeviceException(this,
          new NotSupportedException($"The interface {Interface} is not supported by the device \"{GetType().Name}\"."));

      try
      {
        Session = GlobalResourceManager.Open(ResourceName, AccessModes.ExclusiveLock, ConnectionTimeout);
        DeviceConnectionState = DeviceConnectionState.Initializing;
        await InitializeAsync();
        DeviceConnectionState = DeviceConnectionState.Connected;
      }
      catch (Exception e)
      {
        DeviceConnectionState = DeviceConnectionState.DisconnectedWithError;
        Session?.Dispose();
        throw new VisaDeviceException(this, e);
      }
    }

    /// <inheritdoc />
    public virtual Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task<string> GetIdentifierAsync() => Task.FromResult(AliasName);

    /// <inheritdoc />
    public virtual Task ResetAsync() => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task DeInitializeAsync() => Task.CompletedTask;

    /// <inheritdoc />
    public virtual async Task CloseSessionAsync()
    {
      try
      {
        DeviceConnectionState = DeviceConnectionState.DeInitializing;
        await DeInitializeAsync();
        Session?.Dispose();
      }
      finally
      {
        Session = null;
        DeviceConnectionState = DeviceConnectionState.Disconnected;
      }
    }

    /// <inheritdoc />
    public void Dispose()
    {
      if (_isDisposed)
        return;

      try
      {
        CloseSessionAsync().Wait();
      }
      catch
      {
        // Suppress all exceptions.
      }
      finally
      {
        GC.SuppressFinalize(this);
        _isDisposed = true;
      }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
      if (_isDisposed)
        return;

      try
      {
        await CloseSessionAsync();
      }
      catch
      {
        // Suppress all exceptions.
      }
      finally
      {
        GC.SuppressFinalize(this);
        _isDisposed = true;
      }
    }

    /// <summary>
    ///   Disposes the object on finalization.
    /// </summary>
    ~VisaDevice()
    {
      Dispose();
    }
  }
}
