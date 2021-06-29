using System;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   A builder class that can build message-based VISA devices with custom behavior.
  /// </summary>
  /// <remarks>
  ///   After a VISA device is built, the current builder instance cannot be reused. Create a new builder if necessary.
  /// </remarks>
  public class MessageDeviceBuilder : VisaDeviceBuilder<BuildableMessageDevice, IMessageDevice>
  {
    private Func<IMessageDevice, string, string>? CustomMessageProcessor { get; set; }

    /// <summary>
    ///   Instructs the builder to use the specified message processing delegate to handle request and response
    ///   messages.
    /// </summary>
    /// <param name="messageProcessor">
    ///   A delegate that will handle the common process of message-based communication with the device, and also
    ///   perform additional message checking and formatting if necessary.
    ///   The delegate is provided with a message-based VISA device object and a raw request message string as
    ///   parameters.
    ///   The function must return a processed response message string.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying message-based
    ///   <see cref="IMessageDevice.Session" /> object.
    /// </remarks>
    public MessageDeviceBuilder UseMessageProcessor(Func<IMessageDevice, string, string> messageProcessor)
    {
      CustomMessageProcessor = messageProcessor;
      return this;
    }
  }
}
