using System;
using System.Threading;
using System.Threading.Tasks;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for device action classes.
  /// </summary>
  public interface IDeviceAction : ICloneable
  {
    /// <summary>
    ///   Gets or sets the optional user-readable name of the device action.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    ///   Gets the actual device action delegate this object wraps.
    /// </summary>
    Action DeviceActionDelegate { get; }

    /// <summary>
    ///   Checks if the current device action can be executed at the moment.
    /// </summary>
    bool CanExecute { get; }

    /// <summary>
    ///   The event that is called when an exception occurs during this device action processing.
    ///   The event arguments object contains the exception instance that has raised the event.
    /// </summary>
    event ThreadExceptionEventHandler? Exception;

    /// <summary>
    ///   The event called when this device action execution completes.
    /// </summary>
    event EventHandler? ExecutionCompleted;

    /// <summary>
    ///   Asynchronously executes the current device action with its state being tracked by the
    ///   <see cref="DeviceActionExecutor" /> static class.
    /// </summary>
    Task ExecuteAsync();
  }
}
