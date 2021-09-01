namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The interface for device actions that can be invoked for any owning VISA device.
  ///   The owning VISA device is defined using the <see cref="Owner" /> property.
  /// </summary>
  public interface IOwnedDeviceAction : IDeviceAction
  {
    /// <summary>
    ///   Gets or sets the VISA device instance that owns this device action.
    /// </summary>
    IVisaDevice? Owner { get; set; }
  }
}
