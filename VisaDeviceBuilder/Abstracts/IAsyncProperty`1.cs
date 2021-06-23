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
    /// <inheritdoc cref="IAsyncProperty.Getter" />
    new TValue Getter { get; }

    /// <inheritdoc cref="IAsyncProperty.Setter" />
    new TValue Setter { set; }
  }
}
