using System;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  public class OwnedDeviceAction<TOwner> : DeviceAction, IOwnedDeviceAction<TOwner> where TOwner : IVisaDevice
  {
    /// <inheritdoc />
    public TOwner? Owner { get; set; }

    /// <summary>
    ///   Gets the delegate representing a device action to be asynchronously executed for the <see cref="Owner" />
    ///   VISA device.
    /// </summary>
    private Action<TOwner> OwnedAction { get; }

    /// <inheritdoc />
    protected override Action Action => _action ??= () =>
    {
      if (Owner != null)
        OwnedAction.Invoke(Owner);
    };
    private Action? _action;

    /// <summary>
    ///   Creates a new owned device action instance.
    /// </summary>
    /// <param name="ownedAction">
    ///   The action delegate representing a device action to be asynchronously executed for the <see cref="Owner" />
    ///   VISA device.
    /// </param>
    public OwnedDeviceAction(Action<TOwner> ownedAction) : base(() => {}) => OwnedAction = ownedAction;
  }
}
