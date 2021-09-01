namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The interface for owned asynchronous properties that can be accessed for any owning VISA device.
  ///   The owning VISA device is defined using the <see cref="Owner" /> property.
  /// </summary>
  public interface IOwnedAsyncProperty : IAsyncProperty
  {
    /// <summary>
    ///   Gets or sets the VISA device instance that owns this asynchronous property.
    /// </summary>
    IVisaDevice? Owner { get; set; }
  }
}
