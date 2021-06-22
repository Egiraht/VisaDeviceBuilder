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
    ///   Gets the shared message locking object used for device message requests synchronization.
    /// </summary>
    protected object MessageLock { get; } = new();

    /// <inheritdoc />
    protected override void Initialize()
    {
      if (Session == null)
        throw new VisaDeviceException(this,
          new NotSupportedException($"The device \"{AliasName}\" does not support message-based sessions."));
    }

    /// <inheritdoc />
    /// <exception cref="VisaDeviceException">
    ///   There is no opened VISA session (<see cref="InvalidOperationException" />).
    /// </exception>
    public virtual string SendMessage(string message)
    {
      if (Session == null)
        throw new VisaDeviceException(this,
          new InvalidOperationException("Cannot send a message as there is no opened VISA session."));

      lock (MessageLock)
      {
        Session.FormattedIO.WriteLine(message);
        return Session.FormattedIO.ReadLine().TrimEnd('\x0A');
      }
    }

    /// <inheritdoc />
    /// <exception cref="VisaDeviceException">
    ///   There is no opened VISA session (<see cref="InvalidOperationException" />).
    /// </exception>
    public virtual Task<string> SendMessageAsync(string message) => Task.Run(() => SendMessage(message));
  }
}
