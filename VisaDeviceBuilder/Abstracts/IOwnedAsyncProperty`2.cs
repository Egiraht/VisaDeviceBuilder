namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The interface for owned asynchronous properties with values of type <typeparamref name="TValue" /> that can be
  ///   accessed for any owning VISA device of type <typeparamref name="TOwner" />.
  ///   The owning VISA device is defined using the <see cref="IOwnedAsyncProperty{TOwner}.Owner" /> property.
  /// </summary>
  /// <typeparam name="TOwner">
  ///   The type of a VISA device that can own this asynchronous property.
  ///   It must implement the <see cref="IVisaDevice" /> interface.
  /// </typeparam>
  /// <typeparam name="TValue">
  ///   The type of the value this asynchronous property can access.
  /// </typeparam>
  public interface IOwnedAsyncProperty<TOwner, TValue> : IOwnedAsyncProperty<TOwner>, IAsyncProperty<TValue>
    where TOwner : IVisaDevice
  {
  }
}
