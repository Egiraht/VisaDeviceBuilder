using System;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for builders of message-based VISA devices.
  /// </summary>
  public interface IBuildableMessageDevice : IMessageDevice, IBuildableVisaDevice
  {
    /// <summary>
    ///   Gets or sets the custom device initialization stage callback delegate.
    /// </summary>
    new Action<IMessageDevice>? CustomInitializeCallback { get; set; }

    /// <summary>
    ///   Gets or sets the custom device de-initialization stage callback delegate.
    /// </summary>
    new Action<IMessageDevice>? CustomDeInitializeCallback { get; set; }

    /// <summary>
    ///   Gets or sets the custom delegate for getting the device identifier string.
    /// </summary>
    new Func<IMessageDevice, string>? CustomGetIdentifierCallback { get; set; }

    /// <summary>
    ///   Gets or sets the custom delegate to reset the device.
    /// </summary>
    new Action<IMessageDevice>? CustomResetCallback { get; set; }

    /// <summary>
    ///   Gets or sets the custom delegate for request and response messages processing.
    /// </summary>
    Func<IMessageDevice, string, string>? CustomMessageProcessor { get; set; }
  }
}
