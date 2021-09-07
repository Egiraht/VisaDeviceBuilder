using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
  ///     the asynchronous property while the actual read and write operations are performed asynchronously.
  ///   </para>
  ///   <para>
  ///     The asynchronous property can be read-only, write-only, or read-write depending on the constructor overload
  ///     used for object creation.
  ///   </para>
  /// </summary>
  /// <typeparam name="TValue">
  ///   The type of the value this asynchronous property can access.
  /// </typeparam>
  public class AsyncProperty<TValue> : IAsyncProperty<TValue>
  {
    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public IVisaDevice? TargetDevice { get; set; }

    /// <inheritdoc />
    public bool CanGet { get; }

    /// <inheritdoc />
    public bool CanSet { get; }

    /// <summary>
    ///   Gets the getter delegate to be called when the asynchronous property is read.
    ///   The delegate may accept a nullable VISA device instance from the <see cref="TargetDevice" /> property as a
    ///   parameter, or just reject it if it is not required for functioning.
    ///   The delegate must return a stored property's value of type <typeparamref name="TValue" />.
    /// </summary>
    public virtual Func<IVisaDevice?, TValue> GetterDelegate { get; } = _ => default!;

    /// <summary>
    ///   Gets the setter delegate to be called when the asynchronous property is written.
    ///   The delegate may accept a nullable VISA device instance from the <see cref="TargetDevice" /> property as the
    ///   first parameter, or just reject it if it is not required for functioning.
    ///   As the second parameter the delegate is provided with a new property's value of type
    ///   <typeparamref name="TValue" />.
    /// </summary>
    public virtual Action<IVisaDevice?, TValue> SetterDelegate { get; } = (_, _) => { };

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
    ///   Creates a new read-only asynchronous property of type <typeparamref name="TValue" />.
    /// </summary>
    /// <param name="getterDelegate">
    ///   The getter delegate to be called when the asynchronous property is read.
    ///   The delegate may accept a nullable VISA device instance from the <see cref="TargetDevice" /> property as a
    ///   parameter, or just reject it if it is not required for functioning.
    ///   The delegate must return a stored property's value of type <typeparamref name="TValue" />.
    /// </param>
    public AsyncProperty(Func<IVisaDevice?, TValue> getterDelegate)
    {
      GetterDelegate = getterDelegate;
      CanGet = true;
    }

    /// <summary>
    ///   Creates a new write-only asynchronous property of type <typeparamref name="TValue" />.
    /// </summary>
    /// <param name="setterDelegate">
    ///   The setter delegate to be called when the asynchronous property is written.
    ///   The delegate may accept a nullable VISA device instance from the <see cref="TargetDevice" /> property as the
    ///   first parameter, or just reject it if it is not required for functioning.
    ///   As the second parameter the delegate is provided with a new property's value of type
    ///   <typeparamref name="TValue" />.
    /// </param>
    public AsyncProperty(Action<IVisaDevice?, TValue> setterDelegate)
    {
      SetterDelegate = setterDelegate;
      CanSet = true;
    }

    /// <summary>
    ///   Creates a new read-write asynchronous property of type <typeparamref name="TValue" />.
    /// </summary>
    /// <param name="getterDelegate">
    ///   The getter delegate to be called when the asynchronous property is read.
    ///   The delegate may accept a nullable VISA device instance from the <see cref="TargetDevice" /> property as a
    ///   parameter, or just reject it if it is not required for functioning.
    ///   The delegate must return a stored property's value of type <typeparamref name="TValue" />.
    /// </param>
    /// <param name="setterDelegate">
    ///   The setter delegate to be called when the asynchronous property is written.
    ///   The delegate may accept a nullable VISA device instance from the <see cref="TargetDevice" /> property as the
    ///   first parameter, or just reject it if it is not required for functioning.
    ///   As the second parameter the delegate is provided with a new property's value of type
    ///   <typeparamref name="TValue" />.
    /// </param>
    public AsyncProperty(Func<IVisaDevice?, TValue> getterDelegate, Action<IVisaDevice?, TValue> setterDelegate)
    {
      GetterDelegate = getterDelegate;
      SetterDelegate = setterDelegate;
      CanGet = true;
      CanSet = true;
    }

    /// <summary>
    ///   Converts an object to the value of the <typeparamref name="TValue" /> type.
    ///   The method uses the standard collection of type converters.
    /// </summary>
    /// <param name="value">
    ///   An object to convert.
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
            Getter = GetterDelegate.Invoke(TargetDevice);
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
            SetterDelegate.Invoke(TargetDevice, newValue);
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
    private void OnGetterUpdated() => GetterUpdated?.Invoke(this, EventArgs.Empty);

    /// <summary>
    ///   Calls the <see cref="SetterCompleted" /> event.
    /// </summary>
    private void OnSetterCompleted() => SetterCompleted?.Invoke(this, EventArgs.Empty);

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

    /// <inheritdoc />
    public virtual object Clone() => this switch
    {
      // Read-only asynchronous property:
      {CanGet: true, CanSet: false} => new AsyncProperty<TValue>(GetterDelegate)
      {
        Name = Name,
        TargetDevice = TargetDevice,
        AutoUpdateGetterAfterSetterCompletes = AutoUpdateGetterAfterSetterCompletes
      },

      // Write-only asynchronous property:
      {CanGet: false, CanSet: true} => new AsyncProperty<TValue>(SetterDelegate)
      {
        Name = Name,
        TargetDevice = TargetDevice,
        AutoUpdateGetterAfterSetterCompletes = AutoUpdateGetterAfterSetterCompletes
      },

      // Read-write asynchronous property:
      {CanGet: true, CanSet: true} => new AsyncProperty<TValue>(GetterDelegate, SetterDelegate)
      {
        Name = Name,
        TargetDevice = TargetDevice,
        AutoUpdateGetterAfterSetterCompletes = AutoUpdateGetterAfterSetterCompletes
      },

      // Invalid asynchronous property:
      _ => throw new InvalidOperationException()
    };

    /// <summary>
    ///   Implicitly converts the asynchronous property instance to its getter value.
    /// </summary>
    /// <param name="property">
    ///   The asynchronous property instance to convert.
    /// </param>
    /// <returns>
    ///   The getter value stored in the asynchronous property instance.
    /// </returns>
    [ExcludeFromCodeCoverage]
    public static implicit operator TValue(AsyncProperty<TValue> property) => property.Getter;
  }
}
