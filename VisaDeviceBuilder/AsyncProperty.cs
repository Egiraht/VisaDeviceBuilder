using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The class representing an asynchronous property with a string value.
  /// </summary>
  public class AsyncProperty : IAsyncProperty
  {
    /// <summary>
    ///   Gets or sets the actual value for the <see cref="Getter" /> property.
    /// </summary>
    protected string GetterValue { get; set; } = string.Empty;

    /// <summary>
    ///   Gets or sets the actual value for the <see cref="Setter" /> property.
    /// </summary>
    protected string SetterValue { get; set; } = string.Empty;

    /// <summary>
    ///   Gets or sets the <see cref="Task" /> of the currently running <see cref="Setter" /> property value processing.
    /// </summary>
    protected Task? SetterTask { get; set; }

    /// <inheritdoc />
    public bool CanGet { get; }

    /// <inheritdoc />
    public bool CanSet { get; }

    /// <summary>
    ///   Gets the setter delegate to be called when the asynchronous property is written.
    /// </summary>
    private Action<string>? SetterDelegate { get; }

    /// <summary>
    ///   Gets the getter delegate to be called when the asynchronous property is read.
    /// </summary>
    private Func<string>? GetterDelegate { get; }

    /// <inheritdoc />
    public string Getter => GetterValue;

    /// <inheritdoc />
    public string Setter
    {
      get => SetterValue;
      set
      {
        SetterValue = value;
        SetterTask = ProcessSetterAsync(value);
      }
    }

    /// <summary>
    ///   Gets the shared synchronization lock object.
    /// </summary>
    protected object SynchronizationLock { get; } = new object();

    /// <inheritdoc />
    public event EventHandler? GetterUpdated;

    /// <inheritdoc />
    public event EventHandler? SetterCompleted;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public event ThreadExceptionEventHandler? GetterException;

    /// <inheritdoc />
    public event ThreadExceptionEventHandler? SetterException;

    /// <summary>
    ///   Creates a new get-only asynchronous property of string type.
    /// </summary>
    /// <param name="getterDelegate">
    ///   The getter delegate to be called when the asynchronous property is read.
    /// </param>
    public AsyncProperty(Func<string> getterDelegate)
    {
      CanGet = true;
      GetterDelegate = getterDelegate;
    }

    /// <summary>
    ///   Creates a new set-only asynchronous property of string type.
    /// </summary>
    /// <param name="setterDelegate">
    ///   The setter delegate to be called when the asynchronous property is written.
    /// </param>
    public AsyncProperty(Action<string> setterDelegate)
    {
      CanSet = true;
      SetterDelegate = setterDelegate;
    }

    /// <summary>
    ///   Creates a new get/set asynchronous property of string type.
    /// </summary>
    /// <param name="getterDelegate">
    ///   The getter delegate to be called when the asynchronous property is read.
    /// </param>
    /// <param name="setterDelegate">
    ///   The setter delegate to be called when the asynchronous property is written.
    /// </param>
    public AsyncProperty(Func<string> getterDelegate, Action<string> setterDelegate)
    {
      CanGet = true;
      CanSet = true;
      GetterDelegate = getterDelegate;
      SetterDelegate = setterDelegate;
    }

    /// <inheritdoc />
    public virtual async Task UpdateGetterAsync()
    {
      if (!CanGet)
        return;

      try
      {
        await Task.Run(() =>
        {
          lock (SynchronizationLock)
          {
            GetterValue = GetterDelegate?.Invoke() ?? string.Empty;
          }
        });

        OnPropertyChanged(nameof(Getter));
        OnGetterUpdated();
      }
      catch (Exception e)
      {
        OnGetterException(e);
      }
    }

    /// <summary>
    ///   Asynchronously processes the new value assigned to the the <see cref="Setter" /> property.
    /// </summary>
    /// <param name="value">
    ///   The value passed to the <see cref="Setter" /> property.
    /// </param>
    protected virtual async Task ProcessSetterAsync(string value)
    {
      if (!CanSet)
        return;

      try
      {
        await Task.Run(() =>
        {
          lock (SynchronizationLock)
          {
            SetterDelegate?.Invoke(value);
          }
        });

        SetterValue = string.Empty;
        OnPropertyChanged(nameof(Setter));
        OnSetterCompleted();
      }
      catch (Exception e)
      {
        OnSetterException(e);
      }
    }

    /// <inheritdoc />
    public async Task WaitUntilSetterCompletes() => await (SetterTask ?? Task.CompletedTask);

    /// <summary>
    ///   Calls the <see cref="GetterUpdated" /> event.
    /// </summary>
    protected void OnGetterUpdated() => GetterUpdated?.Invoke(this, new EventArgs());

    /// <summary>
    ///   Calls the <see cref="SetterCompleted" /> event.
    /// </summary>
    protected void OnSetterCompleted() => SetterCompleted?.Invoke(this, new EventArgs());

    /// <summary>
    ///   Calls the <see cref="PropertyChanged" /> event.
    /// </summary>
    /// <param name="propertyName">
    ///   The name of the changed property.
    ///   The calling member name will be used if set to <c>null</c>.
    /// </param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    ///   Calls the <see cref="GetterException" /> event.
    /// </summary>
    /// <param name="exception">
    ///   The exception that caused the getter failure.
    /// </param>
    protected void OnGetterException(Exception exception) =>
      GetterException?.Invoke(this, new ThreadExceptionEventArgs(exception));

    /// <summary>
    ///   Calls the <see cref="SetterException" /> event.
    /// </summary>
    /// <param name="exception">
    ///   The exception that caused the setter failure.
    /// </param>
    protected void OnSetterException(Exception exception) =>
      SetterException?.Invoke(this, new ThreadExceptionEventArgs(exception));
  }
}
