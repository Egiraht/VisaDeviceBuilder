using System.Diagnostics.CodeAnalysis;

namespace VisaDeviceBuilder
{
  // TODO: Switch from "class" to "record" and from "set" to "init" when C# 9 support will be present in the IDE.
  /// <summary>
  ///   Defines the record for storing asynchronous property metadata.
  /// </summary>
  [ExcludeFromCodeCoverage]
  public class AsyncPropertyMetadata
  {
    /// <summary>
    ///   Gets the original name for the asynchronous property defined in the owning class.
    /// </summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    ///   Gets the user-friendly localized name for the asynchronous property.
    /// </summary>
    public string LocalizedName { get; set; } = string.Empty;

    /// <summary>
    ///   Gets the actual asynchronous property instance.
    /// </summary>
    public IAsyncProperty? AsyncProperty { get; set; }
  }
}
