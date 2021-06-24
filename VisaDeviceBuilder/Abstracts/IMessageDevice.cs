using System.Threading.Tasks;
using Ivi.Visa;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The interface describing a message-based VISA device.
  /// </summary>
  public interface IMessageDevice : IVisaDevice
  {
    /// <summary>
    ///   Gets the current message-based VISA session object.
    /// </summary>
    /// <returns>
    ///   The current message-based VISA session object if the VISA device has been successfully connected,
    ///   and the connected device supports message-based communication, otherwise <c>null</c>.
    /// </returns>
    new IMessageBasedSession? Session { get; }

    /// <summary>
    ///   Synchronously sends the message to the connected message-based device.
    /// </summary>
    /// <param name="message">
    ///   The message string to send.
    /// </param>
    /// <returns>
    ///   The message response string returned by the device.
    /// </returns>
    string SendMessage(string message);

    /// <summary>
    ///   Asynchronously sends the message to the connected message-based device.
    /// </summary>
    /// <param name="message">
    ///   The message string to send.
    /// </param>
    /// <returns>
    ///   The message response string returned by the device.
    /// </returns>
    public Task<string> SendMessageAsync(string message);
  }
}
