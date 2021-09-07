using System;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for message-based VISA devices that can be built using builders.
  /// </summary>
  public interface IBuildableMessageDevice : IMessageDevice, IBuildableVisaDevice
  {
    /// <summary>
    ///   Gets or sets the custom delegate for request and response messages processing.
    /// </summary>
    Func<IMessageDevice?, string, string>? CustomMessageProcessor { get; set; }
  }
}
