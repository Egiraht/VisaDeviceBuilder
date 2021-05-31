using System;
using System.Diagnostics.CodeAnalysis;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The attribute indicating that the decorated method can be treated as a device action and
  ///   should be added to the device's <see cref="IVisaDevice.DeviceActions" /> dictionary.
  ///   The decorated method must have a <see cref="Action" /> delegate signature (no parameters and
  ///   no return value).
  /// </summary>
  [AttributeUsage(AttributeTargets.Method), ExcludeFromCodeCoverage]
  public class DeviceActionAttribute : Attribute
  {
    /// <summary>
    ///   Gets or sets the optional name of the device action.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///   Gets or sets the optional user-friendly localized name of the device action.
    /// </summary>
    public string LocalizedName { get; set; } = string.Empty;
  }
}
