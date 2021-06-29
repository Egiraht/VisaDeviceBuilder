using System;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for builders of message-based VISA devices.
  /// </summary>
  /// <typeparam name="TMessageDevice">
  ///   The target message-based VISA device type.
  /// </typeparam>
  public interface IBuildableMessageDevice<TMessageDevice> : IMessageDevice, IBuildableVisaDevice<TMessageDevice>
    where TMessageDevice : IMessageDevice
  {
    /// <summary>
    ///   Gets or sets the custom delegate for request and response messages processing.
    /// </summary>
    Func<TMessageDevice, string, string>? CustomMessageProcessor { get; set; }
  }
}
