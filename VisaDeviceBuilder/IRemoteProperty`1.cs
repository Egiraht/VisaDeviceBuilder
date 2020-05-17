using System;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The generic interface that extends the <see cref="IRemoteProperty" /> interface with custom property
  ///   type support.
  /// </summary>
  /// <typeparam name="TValue">
  ///   The custom type of the remote property.
  /// </typeparam>
  public interface IRemoteProperty<TValue> : IRemoteProperty
  {
    /// <summary>
    ///   Sets the new typed value of the remote property.
    /// </summary>
    TValue TypedSetter { set; }

    /// <summary>
    ///   Gets the current typed value of the remote property.
    /// </summary>
    TValue TypedGetter { get; }

    /// <summary>
    ///   Gets the delegate converting the string value to the value of type <typeparamref name="TValue" />.
    /// </summary>
    Converter<string, TValue> StringToTypeConverter { get; }

    /// <summary>
    ///   Gets the delegate converting the value of type <typeparamref name="TValue" /> to the string value.
    /// </summary>
    Converter<TValue, string> TypeToStringConverter { get; }
  }
}
