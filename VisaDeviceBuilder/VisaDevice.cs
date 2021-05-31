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
    public IResourceManager? ResourceManager { get; }

    /// <inheritdoc />
    public string ResourceName { get; }

    /// <inheritdoc />
    public int ConnectionTimeout { get; set; } = DefaultConnectionTimeout;

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
    public IEnumerable<IAsyncProperty> AsyncProperties { get; }

    /// <inheritdoc />
    public IEnumerable<IDeviceAction> DeviceActions { get; }

    /// <summary>
    ///   Creates a new instance of a custom VISA device.
    /// </summary>
    /// <param name="resourceName">
    ///   The VISA resource name of the device.
    /// </param>
    /// <param name="resourceManager">
    ///   The custom VISA resource manager instance used for VISA session management.
    ///   If set to <c>null</c>, the default <see cref="GlobalResourceManager" /> static class will be used.
    /// </param>
    /// <remarks>
    ///   When using the <see cref="GlobalResourceManager" /> class for VISA resource management with the
    ///   <i>.NET Core</i> runtime, the assembly <i>.dll</i> files of the installed VISA .NET implementations must be
    ///   directly referenced in the project. This is because the <i>.NET Core</i> runtime does not automatically
    ///   locate assemblies from the system's Global Assembly Cache (GAC) used by the <i>.NET Framework</i> runtime,
    ///   and where the VISA standard prescribes to install the VISA .NET implementation libraries.
    /// </remarks>
    public VisaDevice(string resourceName, IResourceManager? resourceManager = null)
    {
      ResourceManager = resourceManager;
      var parsedResourceName = ResourceManager != null
        ? ResourceManager.Parse(resourceName)
        : GlobalResourceManager.Parse(resourceName);

      ResourceName = parsedResourceName.ExpandedUnaliasedName;
      AliasName = parsedResourceName.AliasIfExists;
      Interface = parsedResourceName.InterfaceType;
      AsyncProperties = CollectOwnAsyncProperties();
      DeviceActions = CollectOwnDeviceActions();
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
        result.LocalizedName = !string.IsNullOrWhiteSpace(result.LocalizedName) ? result.LocalizedName : property.Name;
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
          result.LocalizedName =
            !string.IsNullOrWhiteSpace(result.LocalizedName) ? result.LocalizedName : property.Name;
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
              LocalizedName = !string.IsNullOrWhiteSpace(attribute?.LocalizedName)
                ? attribute.LocalizedName
                : method.Name
            };
          }));
    }

    /// <inheritdoc />
    public void OpenSession()
    {
      if (_isDisposed)
        throw new ObjectDisposedException(AliasName);

      if (IsSessionOpened)
        return;

      if (!SupportedInterfaces.Contains(Interface))
        throw new VisaDeviceException(this,
          new NotSupportedException(
            $"The interface \"{Interface}\" is not supported by devices of type \"{GetType().Name}\"."));

      try
      {
        DeviceConnectionState = DeviceConnectionState.Initializing;
        Session = ResourceManager != null
          ? ResourceManager.Open(ResourceName, AccessModes.ExclusiveLock, ConnectionTimeout)
          : GlobalResourceManager.Open(ResourceName, AccessModes.ExclusiveLock, ConnectionTimeout);
        Initialize();
        DeviceConnectionState = DeviceConnectionState.Connected;
      }
      catch (Exception e)
      {
        CloseSession();
        DeviceConnectionState = DeviceConnectionState.DisconnectedWithError;

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
    }

    /// <inheritdoc />
    public virtual string GetIdentifier() => AliasName;

    /// <inheritdoc />
    public virtual Task<string> GetIdentifierAsync() => Task.Run(GetIdentifier);

    /// <inheritdoc />
    [DeviceAction]
    public virtual void Reset()
    {
    }

    /// <inheritdoc />
    public virtual Task ResetAsync() => Task.Run(Reset);

    /// <summary>
    ///   De-initializes the device before the session closing.
    /// </summary>
    protected virtual void DeInitialize()
    {
    }

    /// <inheritdoc />
    public void CloseSession()
    {
      if (_isDisposed)
        throw new ObjectDisposedException(AliasName);

      if (!IsSessionOpened)
        return;

      try
      {
        DeviceConnectionState = DeviceConnectionState.DeInitializing;
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
        DeviceConnectionState = DeviceConnectionState.Disconnected;
      }
    }

    /// <inheritdoc />
    public virtual Task CloseSessionAsync() => Task.Run(CloseSession);

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
