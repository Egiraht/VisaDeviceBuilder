using Ivi.Visa;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The abstract base class for connectable VISA devices that uses message-based communication.
  /// </summary>
  public abstract class MessageDevice : VisaDevice
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
  }
}
