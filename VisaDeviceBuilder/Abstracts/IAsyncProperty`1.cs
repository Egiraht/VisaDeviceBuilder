namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The interface for asynchronous properties with values of type <typeparamref name="TValue" />.
  /// </summary>
  /// <typeparam name="TValue">
  ///   The type of the value this asynchronous property can access.
  /// </typeparam>
  public interface IAsyncProperty<TValue> : IAsyncProperty
  {
    /// <inheritdoc cref="IAsyncProperty.Getter" />
    new TValue Getter { get; }

    /// <inheritdoc cref="IAsyncProperty.Setter" />
    new TValue Setter { set; }
  }
}
