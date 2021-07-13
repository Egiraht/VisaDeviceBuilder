using System;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   A builder class that can build message-based VISA devices with custom behavior.
  /// </summary>
  public class MessageDeviceBuilder : VisaDeviceBuilder<BuildableMessageDevice, IMessageDevice>
  {
    /// <summary>
    ///   Initializes a new message-based VISA device builder instance.
    /// </summary>
    public MessageDeviceBuilder()
    {
    }

    /// <summary>
    ///   Initializes a new message-based VISA device builder instance with building configuration copied from a
    ///   compatible buildable message-based VISA device instance.
    /// </summary>
    /// <param name="device">
    ///   A message-based VISA device instance to copy configuration from. This instance must have been previously built
    ///   by a compatible message-based VISA device builder class and must inherit from the
    ///   <see cref="BuildableMessageDevice" /> class.
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///   Cannot copy building configuration from the provided VISA device instance because it is not assignable to the
    ///   <see cref="BuildableMessageDevice" /> type.
    /// </exception>
    public MessageDeviceBuilder(IMessageDevice device) : base(device)
    {
    }

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
      Device.CustomMessageProcessor = messageProcessor;
      return this;
    }
  }
}
