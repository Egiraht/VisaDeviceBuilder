using System;

namespace VisaDeviceBuilder.WPF.Components
{
  /// <summary>
  ///   Defines the model class containing the device action metadata.
  /// </summary>
  public class DeviceActionMetadata
  {
    /// <summary>
    ///   Gets or sets the original name for the device action defined in the owning class.
    /// </summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    ///   Gets or sets the user-friendly localized name for the device action.
    /// </summary>
    public string LocalizedName { get; set; } = string.Empty;

    /// <summary>
    ///   Gets or sets the actual device action delegate.
    /// </summary>
    public Action? DeviceAction { get; set; }
  }
}
