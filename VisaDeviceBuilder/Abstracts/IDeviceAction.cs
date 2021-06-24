using System;
using System.Threading.Tasks;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for device action classes.
  /// </summary>
  public interface IDeviceAction
  {
    /// <summary>
    ///   Gets or sets the optional user-readable name of the device action.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    ///   Gets the actual device action delegate.
    /// </summary>
    Action Action { get; }

    /// <summary>
    ///   Asynchronously executes the current device action with its state being tracked by the
    ///   <see cref="DeviceActionExecutor" /> static class.
    /// </summary>
    Task ExecuteAsync();
  }
}
