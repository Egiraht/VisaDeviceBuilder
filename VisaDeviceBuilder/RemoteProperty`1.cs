using System;
using System.ComponentModel;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The generic version of <see cref="RemoteProperty" /> class containing a pair of typed accessors
  ///   <see cref="TypedSetter" /> and <see cref="TypedGetter" />.
  /// </summary>
  /// <typeparam name="TValue">
  ///   The type of the remote property.
  /// </typeparam>
  public class RemoteProperty<TValue> : RemoteProperty, IRemoteProperty<TValue>
  {
    /// <inheritdoc />
    public TValue TypedSetter
    {
      get => StringToTypeConverter(Setter);
      set => Setter = TypeToStringConverter(value);
    }

    /// <inheritdoc />
    public TValue TypedGetter
    {
      get => StringToTypeConverter(Getter);
      protected set => Getter = TypeToStringConverter(value);
    }

    /// <inheritdoc />
    public virtual Converter<string, TValue> StringToTypeConverter { get; set; } = DefaultStringToTypeConverter;


    /// <inheritdoc />
    public virtual Converter<TValue, string> TypeToStringConverter { get; set; } = DefaultTypeToStringConverter;

    /// <inheritdoc />
    public RemoteProperty(string name, bool isReadOnly) : base(name, isReadOnly)
    {
    }

    /// <summary>
    ///   Defines the default string to <typeparamref name="TValue" /> type converter.
    ///   Uses the standard collection of type converters for conversion from string.
    /// </summary>
    /// <param name="value">
    ///   The string value to convert.
    /// </param>
    /// <returns>
    ///   A converted value of type <typeparamref name="TValue" />.
    /// </returns>
    private static TValue DefaultStringToTypeConverter(string value)
    {
      var typeConverter = TypeDescriptor.GetConverter(typeof(TValue));
      return typeConverter.CanConvertFrom(typeof(string))
        ? (TValue) typeConverter.ConvertFromInvariantString(value)
        : default!;
    }

    /// <summary>
    ///   Defines the default <typeparamref name="TValue" /> type to string converter.
    ///   Uses the standard <see cref="object.ToString()" /> method for conversion to string.
    /// </summary>
    /// <param name="value">
    ///   The value of type <typeparamref name="TValue" /> to convert.
    /// </param>
    /// <returns>
    ///   A converted string value.
    /// </returns>
    private static string DefaultTypeToStringConverter(TValue value) => value?.ToString() ?? "";
  }
}
