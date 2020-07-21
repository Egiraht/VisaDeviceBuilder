using System;
using System.Threading.Tasks;
using Ivi.Visa;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The class for connectable VISA devices that use message-based communication.
  /// </summary>
  public class MessageDevice : VisaDevice, IMessageDevice
  {
    /// <summary>
    ///   Defines the default collection of supported hardware interface types.
    /// </summary>
    private static readonly HardwareInterfaceType[] SupportedMessageBasedInterfaces =
    {
      HardwareInterfaceType.Gpib,
      HardwareInterfaceType.Serial,
      HardwareInterfaceType.Tcp,
      HardwareInterfaceType.Usb,
      HardwareInterfaceType.Vxi,
      HardwareInterfaceType.GpibVxi
    };

    /// <inheritdoc cref="Session" />
    public new IMessageBasedSession? Session => base.Session as IMessageBasedSession;

    /// <inheritdoc />
    public override HardwareInterfaceType[] SupportedInterfaces => SupportedMessageBasedInterfaces;

    /// <summary>
    ///   Gets the request lock object used for request queue control.
    /// </summary>
    protected object RequestLock { get; } = new object();

    /// <summary>
    ///   Creates a new instance of a custom message-based VISA device.
    /// </summary>
    /// <param name="resourceName">
    ///   The VISA resource name of the device.
    /// </param>
    /// <param name="connectionTimeout">
    ///   The connection timeout in milliseconds.
    ///   Defaults to the <see cref="VisaDevice.DefaultConnectionTimeout" /> value.
    /// </param>
    /// <param name="resourceManager">
    ///   The custom VISA resource manager instance used for VISA session management.
    ///   If set to <c>null</c>, the <see cref="GlobalResourceManager" /> static class will be used.
    /// </param>
    public MessageDevice(string resourceName, int connectionTimeout = DefaultConnectionTimeout,
      IResourceManager? resourceManager = null) : base(resourceName, connectionTimeout, resourceManager)
    {
    }

    /// <inheritdoc />
    public override async Task OpenSessionAsync()
    {
      await base.OpenSessionAsync();

      try
      {
        if (Session == null)
          throw new NotSupportedException($"The device \"{AliasName}\" does not support message-based sessions.");
      }
      catch (Exception e)
      {
        await CloseSessionAsync();
        DeviceConnectionState = DeviceConnectionState.DisconnectedWithError;
        throw new VisaDeviceException(this, e);
      }
    }

    /// <inheritdoc />
    public virtual async Task<string?> SendMessageAsync(string request)
    {
      if (Session == null)
        throw new VisaDeviceException(this,
          new NullReferenceException("Cannot send a message as there is no opened VISA session."));

      return await Task.Run(() =>
      {
        lock (RequestLock)
        {
          Session.FormattedIO.WriteLine(request);
          return Session.FormattedIO.ReadLine().TrimEnd('\x0A');
        }
      });
    }
  }
}
