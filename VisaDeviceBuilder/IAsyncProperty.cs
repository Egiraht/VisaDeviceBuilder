using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The interface for asynchronous properties with string values.
  /// </summary>
  public interface IAsyncProperty : INotifyPropertyChanged
  {
    /// <summary>
    ///   Checks if the asynchronous property can be read.
    /// </summary>
    bool CanGet { get; }

    /// <summary>
    ///   Checks if the asynchronous property can be written.
    /// </summary>
    bool CanSet { get; }

    /// <summary>
    ///   Gets the cached string value of the asynchronous property acquired from the last getter update.
    /// </summary>
    string Getter { get; }

    /// <summary>
    ///   Sets the new string value of the asynchronous property.
    ///   Exceptions thrown during the new value processing can be handled using the <see cref="SetterException" />
    ///   event while this property does not throw any exceptions.
    /// </summary>
    string Setter { get; set; }

    /// <summary>
    ///   Gets of sets the flag indicating if the <see cref="Getter" /> property value should be automatically updated
    ///   after new <see cref="Setter" /> property value processing completes.
    ///   Setting this value to <c>true</c> can be useful if no supplementary <see cref="IAutoUpdater" /> is used
    ///   to periodically update the getter.
    /// </summary>
    bool AutoUpdateGetterAfterSetterCompletes { get; set; }

    /// <summary>
    ///   The event called when the new getter value is updated.
    /// </summary>
    event EventHandler? GetterUpdated;

    /// <summary>
    ///   The event called when the setter value processing is completed.
    /// </summary>
    event EventHandler? SetterCompleted;

    /// <summary>
    ///   The event called on getter failure.
    /// </summary>
    event ThreadExceptionEventHandler? GetterException;

    /// <summary>
    ///   The event called on setter failure.
    /// </summary>
    event ThreadExceptionEventHandler? SetterException;

    /// <summary>
    ///   Requests the asynchronous update of the <see cref="Getter" /> property.
    ///   Exceptions thrown during the update can be handled using the <see cref="GetterException" /> event while
    ///   this method does not throw any exceptions.
    /// </summary>
    void RequestGetterUpdate();

    /// <summary>
    ///   Gets the <see cref="Task" /> object wrapping the asynchronous <see cref="Getter" /> value updating.
    ///   This object can be awaited until the value updating is finished.
    /// </summary>
    /// <returns>
    ///   The running <see cref="Getter" /> updating <see cref="Task" /> object or the
    ///   <see cref="Task.CompletedTask" /> object if no <see cref="Getter" /> updating is running at the moment.
    /// </returns>
    Task GetGetterUpdatingTask();

    /// <summary>
    ///   Gets the <see cref="Task" /> object wrapping the asynchronous new <see cref="Setter" /> value processing.
    ///   This object can be awaited until the value processing is finished.
    /// </summary>
    /// <returns>
    ///   The running <see cref="Setter" /> processing <see cref="Task" /> object or the
    ///   <see cref="Task.CompletedTask" /> object if no <see cref="Setter" /> processing is running at the moment.
    /// </returns>
    Task GetSetterProcessingTask();
  }
}
