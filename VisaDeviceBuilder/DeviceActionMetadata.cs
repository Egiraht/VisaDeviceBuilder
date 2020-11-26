using System;
using System.Diagnostics.CodeAnalysis;

namespace VisaDeviceBuilder
{
  // TODO: Switch from "class" to "record" and from "set" to "init" when C# 9 support will be present in the IDE.
  /// <summary>
  ///   Defines the record for storing device action metadata.
  /// </summary>
  [ExcludeFromCodeCoverage]
  public class DeviceActionMetadata
  {
    /// <summary>
    ///   Gets the original name for the device action defined in the owning class.
    /// </summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    ///   Gets the user-friendly localized name for the device action.
    /// </summary>
    public string LocalizedName { get; set; } = string.Empty;

    /// <summary>
    ///   Gets the actual device action delegate.
    /// </summary>
    public Action? DeviceAction { get; set; }
  }
}
