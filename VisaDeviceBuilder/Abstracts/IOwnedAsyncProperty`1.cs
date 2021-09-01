namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The interface for owned asynchronous properties that can be accessed for any owning VISA device of type
  ///   <typeparamref name="TOwner" />.
  ///   The owning VISA device is defined using the <see cref="Owner" /> property.
  /// </summary>
  /// <typeparam name="TOwner">
  ///   The type of a VISA device that can own this asynchronous property.
  ///   It must implement the <see cref="IVisaDevice" /> interface.
  /// </typeparam>
  public interface IOwnedAsyncProperty<TOwner> : IOwnedAsyncProperty where TOwner : IVisaDevice
  {
    /// <summary>
    ///   Gets or sets the VISA device instance of <typeparamref name="TOwner" /> type that owns this asynchronous
    ///   property.
    /// </summary>
    new TOwner? Owner { get; set; }

    /// <inheritdoc />
    IVisaDevice? IOwnedAsyncProperty.Owner
    {
      get => Owner;
      set => Owner = (TOwner?) value;
    }
  }
}
