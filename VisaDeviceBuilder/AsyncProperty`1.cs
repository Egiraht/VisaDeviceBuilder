using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The class representing an asynchronous property with a value of type <typeparamref name="TValue" />.
  ///   For backward compatibility with the <see cref="AsyncProperty" /> ancestor class the implicit value conversions
  ///   between the <typeparamref name="TValue" /> type and the string type are performed.
  /// </summary>
  /// <typeparam name="TValue">
  ///   Type of the asynchronous property value.
  /// </typeparam>
  public class AsyncProperty<TValue> : AsyncProperty, IAsyncProperty<TValue>
  {
    /// <inheritdoc />
    public new TValue Getter =>
      StringToTypeConverter.Invoke(!string.IsNullOrEmpty(base.Getter) ? base.Getter : default!);

    /// <inheritdoc />
    public new TValue Setter
    {
      get => StringToTypeConverter.Invoke(!string.IsNullOrEmpty(base.Setter) ? base.Setter : default!);
      set => base.Setter = TypeToStringConverter(value) ?? string.Empty;
    }

    /// <summary>
    ///   Gets the delegate converting the value of type <typeparamref name="TValue" /> to the string value.
    /// </summary>
    public Converter<TValue, string> TypeToStringConverter { get; }

    /// <summary>
    ///   Gets the delegate converting the string value to the value of type <typeparamref name="TValue" />.
    /// </summary>
    public Converter<string, TValue> StringToTypeConverter { get; }

    /// <summary>
    ///   Creates a new get-only asynchronous property of type <typeparamref name="TValue" /> with a custom type to
    ///   string value converter.
    /// </summary>
    /// <param name="getterDelegate">
    ///   The getter delegate to be called when the asynchronous property is read.
    /// </param>
    /// <param name="typeToStringConverter">
    ///   The delegate converting the value of type <typeparamref name="TValue" /> to the string value.
    /// </param>
    /// <param name="stringToTypeConverter">
    ///   The delegate converting the string value to the value of type <typeparamref name="TValue" />.
    /// </param>
    public AsyncProperty(Func<string> getterDelegate, Converter<TValue, string> typeToStringConverter,
      Converter<string, TValue> stringToTypeConverter) : base(getterDelegate)
    {
      TypeToStringConverter = typeToStringConverter;
      StringToTypeConverter = stringToTypeConverter;
      GetterValue = TypeToStringConverter(default!);
      SetterValue = TypeToStringConverter(default!);
    }

    /// <summary>
    ///   Creates a new get-only asynchronous property of type <typeparamref name="TValue" /> with a custom type to
    ///   string value converter.
    /// </summary>
    /// <param name="getterDelegate">
    ///   The getter delegate to be called when the asynchronous property is read.
    /// </param>
    /// <param name="typeToStringConverter">
    ///   The delegate converting the value of type <typeparamref name="TValue" /> to the string value.
    /// </param>
    /// <param name="stringToTypeConverter">
    ///   The delegate converting the string value to the value of type <typeparamref name="TValue" />.
    /// </param>
    public AsyncProperty(Func<TValue> getterDelegate, Converter<TValue, string> typeToStringConverter,
      Converter<string, TValue> stringToTypeConverter) :
      this(() => typeToStringConverter(getterDelegate()), typeToStringConverter, stringToTypeConverter)
    {
    }

    /// <summary>
    ///   Creates a new get-only asynchronous property of type <typeparamref name="TValue" />.
    ///   The default type to string converter is used (see the <see cref="DefaultTypeToStringConverter" /> method).
    /// </summary>
    /// <param name="getterDelegate">
    ///   The getter delegate to be called when the asynchronous property is read.
    /// </param>
    public AsyncProperty(Func<string> getterDelegate) :
      this(getterDelegate, DefaultTypeToStringConverter, DefaultStringToTypeConverter)
    {
    }

    /// <summary>
    ///   Creates a new get-only asynchronous property of type <typeparamref name="TValue" />.
    ///   The default type to string converter is used (see the <see cref="DefaultTypeToStringConverter" /> method).
    /// </summary>
    /// <param name="getterDelegate">
    ///   The getter delegate to be called when the asynchronous property is read.
    /// </param>
    public AsyncProperty(Func<TValue> getterDelegate) : this(() => DefaultTypeToStringConverter(getterDelegate()))
    {
    }

    /// <summary>
    ///   Creates a new set-only asynchronous property of type <typeparamref name="TValue" /> with a custom string to
    ///   type value converter.
    /// </summary>
    /// <param name="setterDelegate">
    ///   The setter delegate to be called when the asynchronous property is written.
    /// </param>
    /// <param name="typeToStringConverter">
    ///   The delegate converting the value of type <typeparamref name="TValue" /> to the string value.
    /// </param>
    /// <param name="stringToTypeConverter">
    ///   The delegate converting the string value to the value of type <typeparamref name="TValue" />.
    /// </param>
    public AsyncProperty(Action<string> setterDelegate, Converter<TValue, string> typeToStringConverter,
      Converter<string, TValue> stringToTypeConverter) : base(setterDelegate)
    {
      TypeToStringConverter = typeToStringConverter;
      StringToTypeConverter = stringToTypeConverter;
      GetterValue = TypeToStringConverter(default!);
      SetterValue = TypeToStringConverter(default!);
    }

    /// <summary>
    ///   Creates a new set-only asynchronous property of type <typeparamref name="TValue" /> with a custom string to
    ///   type value converter.
    /// </summary>
    /// <param name="setterDelegate">
    ///   The setter delegate to be called when the asynchronous property is written.
    /// </param>
    /// <param name="typeToStringConverter">
    ///   The delegate converting the value of type <typeparamref name="TValue" /> to the string value.
    /// </param>
    /// <param name="stringToTypeConverter">
    ///   The delegate converting the string value to the value of type <typeparamref name="TValue" />.
    /// </param>
    public AsyncProperty(Action<TValue> setterDelegate, Converter<TValue, string> typeToStringConverter,
      Converter<string, TValue> stringToTypeConverter) :
      this(value => setterDelegate(stringToTypeConverter(value)), typeToStringConverter, stringToTypeConverter)
    {
    }

    /// <summary>
    ///   Creates a new set-only asynchronous property of type <typeparamref name="TValue" />.
    ///   The default string to type converter is used (see the <see cref="DefaultStringToTypeConverter" /> method).
    /// </summary>
    /// <param name="setterDelegate">
    ///   The setter delegate to be called when the asynchronous property is written.
    /// </param>
    public AsyncProperty(Action<string> setterDelegate) :
      this(setterDelegate, DefaultTypeToStringConverter, DefaultStringToTypeConverter)
    {
    }

    /// <summary>
    ///   Creates a new set-only asynchronous property of type <typeparamref name="TValue" />.
    ///   The default string to type converter is used (see the <see cref="DefaultStringToTypeConverter" /> method).
    /// </summary>
    /// <param name="setterDelegate">
    ///   The setter delegate to be called when the asynchronous property is written.
    /// </param>
    public AsyncProperty(Action<TValue> setterDelegate) :
      this(value => setterDelegate(DefaultStringToTypeConverter(value)))
    {
    }

    /// <summary>
    ///   Creates a new get/set asynchronous property of type <typeparamref name="TValue" /> with custom type to string
    ///   and string to type converters.
    /// </summary>
    /// <param name="getterDelegate">
    ///   The getter delegate to be called when the asynchronous property is read.
    /// </param>
    /// <param name="setterDelegate">
    ///   The setter delegate to be called when the asynchronous property is written.
    /// </param>
    /// <param name="typeToStringConverter">
    ///   The delegate converting the value of type <typeparamref name="TValue" /> to the string value.
    /// </param>
    /// <param name="stringToTypeConverter">
    ///   The delegate converting the string value to the value of type <typeparamref name="TValue" />.
    /// </param>
    public AsyncProperty(Func<string> getterDelegate, Action<string> setterDelegate,
      Converter<TValue, string> typeToStringConverter, Converter<string, TValue> stringToTypeConverter) :
      base(getterDelegate, setterDelegate)
    {
      TypeToStringConverter = typeToStringConverter;
      StringToTypeConverter = stringToTypeConverter;
      GetterValue = typeToStringConverter(default!);
      SetterValue = typeToStringConverter(default!);
    }

    /// <summary>
    ///   Creates a new get/set asynchronous property of type <typeparamref name="TValue" /> with custom type to string
    ///   and string to type converters.
    /// </summary>
    /// <param name="getterDelegate">
    ///   The getter delegate to be called when the asynchronous property is read.
    /// </param>
    /// <param name="setterDelegate">
    ///   The setter delegate to be called when the asynchronous property is written.
    /// </param>
    /// <param name="typeToStringConverter">
    ///   The delegate converting the value of type <typeparamref name="TValue" /> to the string value.
    /// </param>
    /// <param name="stringToTypeConverter">
    ///   The delegate converting the string value to the value of type <typeparamref name="TValue" />.
    /// </param>
    public AsyncProperty(Func<TValue> getterDelegate, Action<TValue> setterDelegate,
      Converter<TValue, string> typeToStringConverter, Converter<string, TValue> stringToTypeConverter) :
      this(() => typeToStringConverter(getterDelegate()), value => setterDelegate(stringToTypeConverter(value)),
        typeToStringConverter, stringToTypeConverter)
    {
    }

    /// <summary>
    ///   Creates a new get/set asynchronous property of type <typeparamref name="TValue" />.
    ///   The default type to string and string to type converters are used
    ///   (see the <see cref="DefaultTypeToStringConverter" /> and <see cref="DefaultStringToTypeConverter" /> methods).
    /// </summary>
    /// <param name="getterDelegate">
    ///   The getter delegate to be called when the asynchronous property is read.
    /// </param>
    /// <param name="setterDelegate">
    ///   The setter delegate to be called when the asynchronous property is written.
    /// </param>
    public AsyncProperty(Func<string> getterDelegate, Action<string> setterDelegate) :
      this(getterDelegate, setterDelegate, DefaultTypeToStringConverter, DefaultStringToTypeConverter)
    {
    }

    /// <summary>
    ///   Creates a new get/set asynchronous property of type <typeparamref name="TValue" />.
    ///   The default type to string and string to type converters are used
    ///   (see the <see cref="DefaultTypeToStringConverter" /> and <see cref="DefaultStringToTypeConverter" /> methods).
    /// </summary>
    /// <param name="getterDelegate">
    ///   The getter delegate to be called when the asynchronous property is read.
    /// </param>
    /// <param name="setterDelegate">
    ///   The setter delegate to be called when the asynchronous property is written.
    /// </param>
    public AsyncProperty(Func<TValue> getterDelegate, Action<TValue> setterDelegate) :
      this(() => DefaultTypeToStringConverter(getterDelegate()),
        value => setterDelegate(DefaultStringToTypeConverter(value)))
    {
    }

    /// <inheritdoc />
    protected override async Task ProcessSetterAsync(string value)
    {
      await base.ProcessSetterAsync(value);
      SetterValue = TypeToStringConverter(default!);
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
    public static TValue DefaultStringToTypeConverter(string value)
    {
      try
      {
        var typeConverter = TypeDescriptor.GetConverter(typeof(TValue));
        return typeConverter.CanConvertFrom(typeof(string))
          ? (TValue) typeConverter.ConvertFromInvariantString(value)!
          : default!;
      }
      catch
      {
        return default!;
      }
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
    public static string DefaultTypeToStringConverter(TValue value) => value is IConvertible convertible
      ? convertible.ToString(CultureInfo.InvariantCulture)
      : value?.ToString() ?? "";
  }
}
