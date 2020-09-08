using System.Threading.Tasks;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The interface describing a message-based VISA device.
  /// </summary>
  public interface IMessageDevice : IVisaDevice
  {
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
    Task<string> SendMessageAsync(string message);
  }
}
