namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The interface for device actions that can be invoked for any owning VISA device of type
  ///   <typeparamref name="TOwner" />.
  ///   The owning VISA device is defined using the <see cref="Owner" /> property.
  /// </summary>
  /// <typeparam name="TOwner">
  ///   The type of a VISA device that can own this device action.
  ///   It must implement the <see cref="IVisaDevice" /> interface.
  /// </typeparam>
  public interface IOwnedDeviceAction<TOwner> : IDeviceAction where TOwner : IVisaDevice
  {
    /// <summary>
    ///   Gets or sets the VISA device instance of <typeparamref name="TOwner" /> type that owns this device action.
    /// </summary>
    TOwner? Owner { get; set; }
  }
}
