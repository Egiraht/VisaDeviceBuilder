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
    /// <param name="resourceManager">
    ///   The custom VISA resource manager instance used for VISA session management.
    ///   If set to <c>null</c>, the <see cref="GlobalResourceManager" /> static class will be used.
    /// </param>
    public MessageDevice(string resourceName, IResourceManager? resourceManager = null) :
      base(resourceName, resourceManager)
    {
    }

    /// <inheritdoc />
    protected override Task InitializeAsync() => Session != null
      ? Task.CompletedTask
      : throw new VisaDeviceException(this,
        new NotSupportedException($"The device \"{AliasName}\" does not support message-based sessions."));

    /// <inheritdoc />
    /// <exception cref="VisaDeviceException">
    ///   There is no opened VISA session (<see cref="InvalidOperationException" />).
    /// </exception>
    public virtual string SendMessage(string message)
    {
      if (Session == null)
        throw new VisaDeviceException(this,
          new InvalidOperationException("Cannot send a message as there is no opened VISA session."));

      lock (RequestLock)
      {
        Session.FormattedIO.WriteLine(message);
        return Session.FormattedIO.ReadLine().TrimEnd('\x0A');
      }
    }

    /// <inheritdoc />
    public virtual Task<string> SendMessageAsync(string message) => Task.Run(() => SendMessage(message));
  }
}
