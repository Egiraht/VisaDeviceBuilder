using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   <para>
  ///     The class representing an asynchronous property with a value of type <typeparamref name="TValue" />.
  ///   </para>
  ///   <para>
  ///     The <see cref="Getter" /> and <see cref="Setter" /> properties represent the corresponding value accessors of
  ///     the asynchronous property while the actual read and write operations are performed asynchronously according
  ///     to the callback functions provided to the constructor.
  ///   </para>
  ///   <para>
  ///     The asynchronous property can be read-only, write-only, or read-write depending on the constructor overload
  ///     used for object creation.
  ///   </para>
  /// </summary>
  /// <typeparam name="TValue">
  ///   Type of the asynchronous property value.
  /// </typeparam>
  public class AsyncProperty<TValue> : IAsyncProperty<TValue>
  {
    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public bool CanGet { get; } = false;

    /// <inheritdoc />
    public bool CanSet { get; } = false;

    /// <summary>
    ///   Gets the getter delegate to be called when the asynchronous property is read.
    /// </summary>
    private Func<TValue> GetterDelegate { get; } = () => default!;

    /// <summary>
    ///   Gets the setter delegate to be called when the asynchronous property is written.
    /// </summary>
    private Action<TValue> SetterDelegate { get; } = _ => { };

    /// <inheritdoc />
    public TValue Getter { get; private set; } = default!;

    /// <inheritdoc />
    object? IAsyncProperty.Getter => Getter;

    /// <inheritdoc />
    public TValue Setter
    {
      set => ProcessSetter(value);
    }

    /// <inheritdoc />
    object? IAsyncProperty.Setter
    {
      set => Setter = ConvertValueFromObject(value);
    }

    /// <inheritdoc />
    public Type ValueType => typeof(TValue);

    /// <summary>
    ///   Gets or sets the <see cref="Task" /> of the currently running <see cref="Getter" /> property value updating.
    /// </summary>
    private Task? GetterTask { get; set; }

    /// <summary>
    ///   Gets or sets the <see cref="Task" /> of the currently running <see cref="Setter" /> property value processing.
    /// </summary>
    private Task? SetterTask { get; set; }

    /// <inheritdoc />
    public bool AutoUpdateGetterAfterSetterCompletes { get; set; } = true;

    /// <summary>
    ///   Gets the shared synchronization lock object.
    /// </summary>
    private object SynchronizationLock { get; } = new();

    /// <inheritdoc />
    public event EventHandler? GetterUpdated;

    /// <inheritdoc />
    public event EventHandler? SetterCompleted;

    /// <inheritdoc />
    public event ThreadExceptionEventHandler? GetterException;

    /// <inheritdoc />
    public event ThreadExceptionEventHandler? SetterException;

    /// <summary>
    ///   Creates a new get-only asynchronous property of type <typeparamref name="TValue" /> with a custom type to
    ///   string value converter.
    /// </summary>
    /// <param name="getterDelegate">
    ///   The getter delegate to be called when the asynchronous property is read.
    /// </param>
    public AsyncProperty(Func<TValue> getterDelegate)
    {
      GetterDelegate = getterDelegate;
      CanGet = true;
    }

    /// <summary>
    ///   Creates a new set-only asynchronous property of type <typeparamref name="TValue" /> with a custom string to
    ///   type value converter.
    /// </summary>
    /// <param name="setterDelegate">
    ///   The setter delegate to be called when the asynchronous property is written.
    /// </param>
    public AsyncProperty(Action<TValue> setterDelegate)
    {
      SetterDelegate = setterDelegate;
      CanSet = true;
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
    public AsyncProperty(Func<TValue> getterDelegate, Action<TValue> setterDelegate)
    {
      GetterDelegate = getterDelegate;
      SetterDelegate = setterDelegate;
      CanGet = true;
      CanSet = true;
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
    private static TValue ConvertValueFromObject(object? value)
    {
      if (value == null)
        return default!;

      if (value.GetType() == typeof(TValue))
        return (TValue) value;

      try
      {
        var typeConverter = TypeDescriptor.GetConverter(typeof(TValue));
        return typeConverter.CanConvertFrom(typeof(string))
          ? (TValue) typeConverter.ConvertFrom(value)!
          : default!;
      }
      catch
      {
        return default!;
      }
    }

    /// <inheritdoc />
    public void RequestGetterUpdate()
    {
      if (GetterTask != null)
        return;

      lock (SynchronizationLock)
      {
        GetterTask = Task.Run(() =>
        {
          if (!CanGet)
            return;

          try
          {
            Getter = GetterDelegate.Invoke();
            OnGetterUpdated();
          }
          catch (Exception e)
          {
            OnGetterException(e);
          }
          finally
          {
            GetterTask = null;
          }
        });
      }
    }

    /// <inheritdoc />
    public Task GetGetterUpdatingTask() => GetterTask ?? Task.CompletedTask;

    /// <summary>
    ///   Processes the new value assigned to the <see cref="Setter" /> property.
    /// </summary>
    /// <param name="newValue">
    ///   The value passed to the <see cref="Setter" /> property.
    /// </param>
    private void ProcessSetter(TValue newValue)
    {
      lock (SynchronizationLock)
      {
        SetterTask = Task.Run(() =>
        {
          if (!CanSet)
            return;

          try
          {
            SetterDelegate.Invoke(newValue);
            OnSetterCompleted();

            if (AutoUpdateGetterAfterSetterCompletes)
              RequestGetterUpdate();
          }
          catch (Exception e)
          {
            OnSetterException(e);
          }
          finally
          {
            SetterTask = null;
          }
        });
      }
    }

    /// <inheritdoc />
    public Task GetSetterProcessingTask() => SetterTask ?? Task.CompletedTask;

    /// <summary>
    ///   Calls the <see cref="GetterUpdated" /> event.
    /// </summary>
    private void OnGetterUpdated() => GetterUpdated?.Invoke(this, new EventArgs());

    /// <summary>
    ///   Calls the <see cref="SetterCompleted" /> event.
    /// </summary>
    private void OnSetterCompleted() => SetterCompleted?.Invoke(this, new EventArgs());

    /// <summary>
    ///   Calls the <see cref="GetterException" /> event.
    /// </summary>
    /// <param name="exception">
    ///   The exception that caused the getter failure.
    /// </param>
    private void OnGetterException(Exception exception) =>
      GetterException?.Invoke(this, new ThreadExceptionEventArgs(exception));

    /// <summary>
    ///   Calls the <see cref="SetterException" /> event.
    /// </summary>
    /// <param name="exception">
    ///   The exception that caused the setter failure.
    /// </param>
    private void OnSetterException(Exception exception) =>
      SetterException?.Invoke(this, new ThreadExceptionEventArgs(exception));

    /// <summary>
    ///   Implicitly converts the asynchronous property instance to its getter value.
    /// </summary>
    /// <param name="property">
    ///   The asynchronous property instance to convert.
    /// </param>
    /// <returns>
    ///   The getter value stored in the asynchronous property instance.
    /// </returns>
    public static implicit operator TValue(AsyncProperty<TValue> property) => property.Getter;
  }
}
