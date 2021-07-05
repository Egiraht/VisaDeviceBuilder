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
    private Action<TOwner> OwnedDeviceActionDelegate { get; }

    /// <inheritdoc />
    public override Action DeviceActionDelegate => _deviceActionDelegate ??= () =>
    {
      if (Owner != null)
        OwnedDeviceActionDelegate.Invoke(Owner);
    };
    private Action? _deviceActionDelegate;

    /// <summary>
    ///   Creates a new owned device action instance.
    /// </summary>
    /// <param name="ownedActionDelegate">
    ///   The action delegate representing a device action to be asynchronously executed for the <see cref="Owner" />
    ///   VISA device.
    /// </param>
    public OwnedDeviceAction(Action<TOwner> ownedActionDelegate) : base(() => { }) =>
      OwnedDeviceActionDelegate = ownedActionDelegate;

    /// <inheritdoc />
    public override object Clone() => new OwnedDeviceAction<TOwner>(OwnedDeviceActionDelegate)
    {
      Name = Name,
      Owner = Owner
    };
  }
}
