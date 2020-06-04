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
    public new IMessageBasedSession? Session
    {
      get => base.Session as IMessageBasedSession;
      protected set => base.Session = value;
    }

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
    public MessageDevice(string resourceName, int connectionTimeout = DefaultConnectionTimeout) :
      base(resourceName, connectionTimeout)
    {
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
          return Session.FormattedIO.ReadLine();
        }
      });
    }
  }
}
