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
    /// <exception cref="InvalidOperationException">
    ///   No owning VISA device is specified for this owned device action.
    /// </exception>
    public override Action DeviceActionDelegate => _deviceActionDelegate ??= () =>
    {
      ThrowIfNoOwnerSpecified();
      OwnedDeviceActionDelegate.Invoke(Owner!);
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

    /// <summary>
    ///   Throws an <see cref="InvalidOperationException" /> if <see cref="Owner" /> is <c>null</c>.
    /// </summary>
    private void ThrowIfNoOwnerSpecified()
    {
      if (Owner == null)
        throw new InvalidOperationException(
          $"No owning VISA device is specified for the owned device action \"{Name}\".");
    }

    /// <inheritdoc />
    public override object Clone() => new OwnedDeviceAction<TOwner>(OwnedDeviceActionDelegate)
    {
      Name = Name,
      Owner = Owner
    };
  }
}
