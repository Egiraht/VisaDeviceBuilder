using System;
using System.Diagnostics.CodeAnalysis;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   Defines the record for storing device action metadata.
  /// </summary>
  [ExcludeFromCodeCoverage]
  public record DeviceActionMetadata
  {
    /// <summary>
    ///   Gets the original name for the device action defined in the owning class.
    /// </summary>
    public string OriginalName { get; init; } = string.Empty;

    /// <summary>
    ///   Gets the user-friendly localized name for the device action.
    /// </summary>
    public string LocalizedName { get; init; } = string.Empty;

    /// <summary>
    ///   Gets the actual device action delegate.
    /// </summary>
    public Action DeviceAction { get; init; } = () => { };
  }
}
