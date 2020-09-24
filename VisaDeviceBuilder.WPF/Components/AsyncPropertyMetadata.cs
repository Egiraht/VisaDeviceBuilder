using System.Diagnostics.CodeAnalysis;

namespace VisaDeviceBuilder.WPF.Components
{
  /// <summary>
  ///   Defines the model class containing the asynchronous property metadata.
  /// </summary>
  [ExcludeFromCodeCoverage]
  public class AsyncPropertyMetadata
  {
    /// <summary>
    ///   Gets or sets the original name for the asynchronous property defined in the owning class.
    /// </summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    ///   Gets or sets the user-friendly localized name for the asynchronous property.
    /// </summary>
    public string LocalizedName { get; set; } = string.Empty;

    /// <summary>
    ///   Gets or sets the actual asynchronous property instance.
    /// </summary>
    public IAsyncProperty? AsyncProperty { get; set; }
  }
}
