namespace VisaDeviceBuilder.WPF.Components
{
  /// <summary>
  ///   Defines the model class containing the asynchronous action metadata.
  /// </summary>
  public class AsyncActionMetadata
  {
    /// <summary>
    ///   Gets or sets the user-friendly name for the asynchronous action.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///   Gets or sets the actual asynchronous action delegate.
    /// </summary>
    public AsyncAction? AsyncAction { get; set; }
  }
}
