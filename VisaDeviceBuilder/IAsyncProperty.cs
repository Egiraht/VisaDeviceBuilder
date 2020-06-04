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
    ///   After the operation is completed the <see cref="Getter" /> property value is updated automatically.
    /// </summary>
    Task UpdateGetterAsync();

    /// <summary>
    ///   Waits until the <see cref="Setter" /> property value processing is completed.
    /// </summary>
    Task WaitUntilSetterCompletes();
  }
}
