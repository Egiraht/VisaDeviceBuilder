using System;
using System.Linq;
using System.Threading.Tasks;
using Ivi.Visa;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The abstract base class for connectable VISA devices.
  /// </summary>
  public abstract class VisaDevice : IVisaDevice
  {
    /// <summary>
    ///   The backing field for <see cref="ResourceName" /> property.
    /// </summary>
    private string _resourceName = "";

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
    public DeviceConnectionState DeviceConnectionState { get; } = DeviceConnectionState.Disconnected;

    /// <inheritdoc />
    public IVisaSession? Session { get; protected set; }

    /// <inheritdoc />
    public string ResourceName
    {
      get => _resourceName;
      protected set
      {
        if (DeviceConnectionState != DeviceConnectionState.Disconnected &&
          DeviceConnectionState != DeviceConnectionState.DisconnectedWithError)
          return;

        if (!GlobalResourceManager.TryParse(value, out var result))
          return;

        _resourceName = result.ExpandedUnaliasedName;
        AliasName = result.AliasIfExists;
        Interface = result.InterfaceType;
      }
    }

    /// <inheritdoc />
    public string AliasName { get; private set; } = "";

    /// <inheritdoc />
    public HardwareInterfaceType Interface { get; private set; } = HardwareInterfaceType.Custom;

    /// <inheritdoc />
    public virtual HardwareInterfaceType[] SupportedInterfaces => DefaultSupportedInterfaces;

    /// <inheritdoc />
    public bool IsInitialized { get; protected set; } = false;

    /// <inheritdoc />
    public IRemoteProperty[] RemoteProperties { get; }

    /// <summary>
    ///   Creates a new instance of VISA device.
    /// </summary>
    protected VisaDevice()
    {
      RemoteProperties = GetType()
        .GetProperties()
        .Where(property => typeof(IRemoteProperty).IsAssignableFrom(property.PropertyType) && property.CanRead)
        .Select(property => (IRemoteProperty) property.GetValue(this))
        .ToArray();
    }

    /// <inheritdoc />
    public abstract Task OpenSessionAsync();

    /// <inheritdoc />
    public virtual Task InitializeAsync()
    {
      IsInitialized = true;
      return Task.CompletedTask;
    }

    /// <inheritdoc />
    public abstract Task<string> GetIdentifierAsync();

    /// <inheritdoc />
    public abstract Task ResetAsync();

    /// <inheritdoc />
    public virtual Task DeInitializeAsync()
    {
      IsInitialized = false;
      return Task.CompletedTask;
    }

    /// <inheritdoc />
    public abstract Task CloseSessionAsync();

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
