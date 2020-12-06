using System.Diagnostics.CodeAnalysis;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   Defines the record for storing asynchronous property metadata.
  /// </summary>
  [ExcludeFromCodeCoverage]
  public record AsyncPropertyMetadata
  {
    /// <summary>
    ///   Gets the original name for the asynchronous property defined in the owning class.
    /// </summary>
    public string OriginalName { get; init; } = string.Empty;

    /// <summary>
    ///   Gets the user-friendly localized name for the asynchronous property.
    /// </summary>
    public string LocalizedName { get; init; } = string.Empty;

    /// <summary>
    ///   Gets the actual asynchronous property instance.
    /// </summary>
    public IAsyncProperty AsyncProperty { get; init; } = new AsyncProperty(() => string.Empty);
  }
}
