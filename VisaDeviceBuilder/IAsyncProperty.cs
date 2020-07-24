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
    ///   Gets the current string value of the asynchronous property.
    /// </summary>
    string Getter { get; }

    /// <summary>
    ///   Sets the new string value of the asynchronous property.
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
    ///   Performs the asynchronous update of the <see cref="Getter" /> property.
    /// </summary>
    Task UpdateGetterAsync();

    /// <summary>
    ///   Waits until <see cref="Setter" /> property value processing completes.
    /// </summary>
    Task WaitUntilSetterCompletes();
  }
}
