using System;
using System.Threading.Tasks;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

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
    public static readonly HardwareInterfaceType[] DefaultSupportedMessageBasedInterfaces =
    {
      HardwareInterfaceType.Gpib,
      HardwareInterfaceType.Serial,
      HardwareInterfaceType.Tcp,
      HardwareInterfaceType.Usb,
      HardwareInterfaceType.Vxi,
      HardwareInterfaceType.GpibVxi
    };

    /// <inheritdoc />
    public new IMessageBasedSession? Session => (IMessageBasedSession?) base.Session;

    /// <inheritdoc />
    public override HardwareInterfaceType[] SupportedInterfaces => DefaultSupportedMessageBasedInterfaces;

    /// <inheritdoc />
    /// <exception cref="VisaDeviceException">
    ///   The connected device does not support message-based VISA sessions.
    /// </exception>
    protected override void Initialize()
    {
      if (base.Session is not IMessageBasedSession)
        throw new VisaDeviceException(this, new NotSupportedException(
          $"The connected device \"{AliasName}\" does not support message-based VISA sessions."));

      lock (SessionLock)
      {
      }
    }

    /// <inheritdoc />
    /// <exception cref="VisaDeviceException">
    ///   Cannot send a message as there is no opened VISA session.
    /// </exception>
    public virtual string SendMessage(string message)
    {
      ThrowWhenNoVisaSessionIsOpened();

      lock (SessionLock)
      {
        Session!.FormattedIO.WriteLine(message);
        return Session.FormattedIO.ReadLine().TrimEnd('\x0A');
      }
    }

    /// <inheritdoc />
    /// <exception cref="VisaDeviceException">
    ///   Cannot send a message as there is no opened VISA session.
    /// </exception>
    public virtual Task<string> SendMessageAsync(string message) => Task.Run(() => SendMessage(message));
  }
}
