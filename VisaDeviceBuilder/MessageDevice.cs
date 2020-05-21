using System.Threading.Tasks;
using Ivi.Visa;
using VisaDeviceBuilder.Exceptions;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The abstract base class for connectable VISA devices that uses message-based communication.
  /// </summary>
  public abstract class MessageDevice : VisaDevice, IMessageRequestProvider
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

    /// <inheritdoc />
    public virtual async Task<string> SendRequestAsync(string request)
    {
      if (Session == null)
        throw new VisaSessionException(this);

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
