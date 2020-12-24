using System;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The class representing an action that can be asynchronously executed by a particular VISA device.
  /// </summary>
  public class DeviceAction : IDeviceAction
  {
    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string LocalizedName { get; set; } = string.Empty;

    /// <inheritdoc />
    public Action Action { get; }

    /// <summary>
    ///   Creates a new device action instance.
    /// </summary>
    /// <param name="action">
    ///   The action delegate representing a device action to be asynchronously executed by a device.
    /// </param>
    public DeviceAction(Action action) => Action = action;

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
