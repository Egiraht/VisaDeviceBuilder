using System;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The class representing an action that can be asynchronously executed by a VISA device.
  ///   The special <see cref="DeviceActionExecutor" /> static class can help to track execution states of device
  ///   actions.
  /// </summary>
  public class DeviceAction : IDeviceAction
  {
    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///   Gets the delegate representing a device action to be asynchronously executed.
    /// </summary>
    protected virtual Action Action { get; }

    /// <summary>
    ///   Creates a new device action instance.
    /// </summary>
    /// <param name="action">
    ///   The action delegate representing a device action to be asynchronously executed.
    /// </param>
    public DeviceAction(Action action) => Action = action;

    /// <inheritdoc />
    public Task ExecuteAsync()
    {
      DeviceActionExecutor.Execute(this);
      return DeviceActionExecutor.GetDeviceActionTask(this);
    }

    /// <summary>
    ///   Implicitly converts the provided device action instance to its action delegate.
    /// </summary>
    /// <param name="action">
    ///   The device action instance to convert.
    /// </param>
    /// <returns>
    ///   The getter value string stored in the provided asynchronous property instance.
    /// </returns>
    public static implicit operator Action(DeviceAction action) => action.Action;
  }
}
