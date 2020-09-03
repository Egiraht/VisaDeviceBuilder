namespace VisaDeviceBuilder.WPF.Components
{
  /// <summary>
  ///   Defines the model class containing the asynchronous action metadata.
  /// </summary>
  public class AsyncActionMetadata
  {
    /// <summary>
    ///   Gets or sets the original name for the asynchronous action defined in the owning class.
    /// </summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    ///   Gets or sets the user-friendly localized name for the asynchronous action.
    /// </summary>
    public string LocalizedName { get; set; } = string.Empty;

    /// <summary>
    ///   Gets or sets the actual asynchronous action delegate.
    /// </summary>
    public AsyncAction? AsyncAction { get; set; }
  }
}
