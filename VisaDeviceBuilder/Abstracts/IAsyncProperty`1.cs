namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The interface for asynchronous properties with values of type <typeparamref name="TValue" />.
  /// </summary>
  /// <typeparam name="TValue">
  ///   Type of the asynchronous property value.
  /// </typeparam>
  public interface IAsyncProperty<TValue> : IAsyncProperty
  {
    /// <summary>
    ///   Gets the cached value of the asynchronous property acquired from the last getter update.
    /// </summary>
    new TValue Getter { get; }

    /// <summary>
    ///   Sets the new value of the asynchronous property.
    ///   Exceptions thrown during the new value processing can be handled using the
    ///   <see cref="IAsyncProperty.SetterException" /> event.
    /// </summary>
    new TValue Setter { get; set; }
  }
}
