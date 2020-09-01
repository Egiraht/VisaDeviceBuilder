namespace VisaDeviceBuilder.WPF.Components
{
  /// <summary>
  ///   Defines the model class containing the asynchronous property metadata.
  /// </summary>
  public class AsyncPropertyMetadata
  {
    /// <summary>
    ///   Gets or sets the user-friendly name for the asynchronous property.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///   Gets or sets the actual asynchronous property instance.
    /// </summary>
    public IAsyncProperty? AsyncProperty { get; set; }
  }
}
