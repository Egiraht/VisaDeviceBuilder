using System;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The common interface for device action classes.
  /// </summary>
  public interface IDeviceAction
  {
    /// <summary>
    ///   Gets or sets the optional name of the device action.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    ///   Gets or sets the optional user-friendly localized name of the device action.
    /// </summary>
    string LocalizedName { get; set; }

    /// <summary>
    ///   Gets the actual device action delegate.
    /// </summary>
    Action Action { get; }
  }
}
