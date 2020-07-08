namespace VisaDeviceBuilder
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
    ///   Gets the current value of the asynchronous property.
    /// </summary>
    new TValue Getter { get; }

    /// <summary>
    ///   Sets the new value of the asynchronous property.
    /// </summary>
    new TValue Setter { get; set; }
  }
}
