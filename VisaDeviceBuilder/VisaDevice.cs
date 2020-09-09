using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ivi.Visa;

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
    public IDictionary<string, IAsyncProperty> AsyncProperties { get; }

    /// <inheritdoc />
    public IDictionary<string, AsyncAction> AsyncActions { get; }

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
      AsyncActions = CollectOwnAsyncActions();
    }

    /// <summary>
    ///   Collects all asynchronous properties defined in the current device class.
    /// </summary>
    /// <returns>
    ///   The dictionary with the collected asynchronous properties.
    ///   Keys of the dictionary contain the names of corresponding asynchronous properties stored as values.
    /// </returns>
    private IDictionary<string, IAsyncProperty> CollectOwnAsyncProperties() =>
      GetType()
        .GetProperties()
        .Where(property => typeof(IAsyncProperty).IsAssignableFrom(property.PropertyType) && property.CanRead)
        .ToDictionary(property => property.Name, property => (IAsyncProperty) property.GetValue(this));

    /// <summary>
    ///   Collects all asynchronous actions defined in the current device class that represent class methods having
    ///   the <see cref="AsyncAction" /> signature and decorated with <see cref="AsyncActionAttribute" /> attribute.
    /// </summary>
    /// <returns>
    ///   The dictionary with the collected asynchronous actions.
    ///   Keys of the dictionary contain the names of corresponding asynchronous actions stored as values.
    /// </returns>
    private IDictionary<string, AsyncAction> CollectOwnAsyncActions() =>
      GetType()
        .GetMethods()
        .Where(method => Utilities.ValidateDelegate<AsyncAction>(method) &&
          method.GetCustomAttribute<AsyncActionAttribute>() != null)
        .ToDictionary(method => method.Name, method => (AsyncAction) method.CreateDelegate(typeof(AsyncAction), this));

    /// <inheritdoc />
    public async Task OpenSessionAsync()
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
        await Task.Run(() =>
        {
          Session = ResourceManager != null
            ? ResourceManager.Open(ResourceName, AccessModes.ExclusiveLock, ConnectionTimeout)
            : GlobalResourceManager.Open(ResourceName, AccessModes.ExclusiveLock, ConnectionTimeout);
        });
        await InitializeAsync();
        DeviceConnectionState = DeviceConnectionState.Connected;
      }
      catch (Exception e)
      {
        await CloseSessionAsync();
        DeviceConnectionState = DeviceConnectionState.DisconnectedWithError;

        throw e is VisaDeviceException visaDeviceException
          ? visaDeviceException
          : new VisaDeviceException(this, e);
      }
    }

    /// <summary>
    ///   Asynchronously initializes the device after the successful session opening.
    /// </summary>
    protected virtual Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task<string> GetIdentifierAsync() => Task.FromResult(AliasName);

    /// <inheritdoc />
    [AsyncAction]
    public virtual Task ResetAsync() => Task.CompletedTask;

    /// <summary>
    ///   Asynchronously de-initializes the device before the session closing.
    /// </summary>
    protected virtual Task DeInitializeAsync() => Task.CompletedTask;

    /// <inheritdoc />
    public async Task CloseSessionAsync()
    {
      if (_isDisposed)
        throw new ObjectDisposedException(AliasName);

      if (!IsSessionOpened)
        return;

      try
      {
        DeviceConnectionState = DeviceConnectionState.DeInitializing;
        await DeInitializeAsync();
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
    public virtual void Dispose() => Task.Run(DisposeAsync).Wait();

    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
      if (_isDisposed)
        return;

      await CloseSessionAsync();

      GC.SuppressFinalize(this);
      _isDisposed = true;
    }

    /// <summary>
    ///   Disposes the object on finalization.
    /// </summary>
    [ExcludeFromCodeCoverage]
    ~VisaDevice()
    {
      Dispose();
    }
  }
}
