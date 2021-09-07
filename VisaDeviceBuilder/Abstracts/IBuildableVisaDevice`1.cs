using System;
using System.Collections.ObjectModel;
using Ivi.Visa;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for VISA devices that can be built using builders.
  /// </summary>
  /// <typeparam name="TVisaDevice">
  ///   The target VISA device type.
  /// </typeparam>
  public interface IBuildableVisaDevice<TVisaDevice> : IVisaDevice where TVisaDevice : IVisaDevice
  {
    /// <summary>
    ///   Gets the array of custom hardware interfaces supported by the device.
    ///   If set to <c>null</c>, the default hardware interfaces defined for the <typeparamref name="TVisaDevice" />
    ///   type will be supported.
    /// </summary>
    HardwareInterfaceType[]? CustomSupportedInterfaces { get; set; }

    /// <summary>
    ///   Gets the observable collection of custom asynchronous properties.
    ///   The <see cref="IAsyncProperty.TargetDevice" /> properties of asynchronous properties being added to the
    ///   collection will be automatically assigned.
    /// </summary>
    ObservableCollection<IAsyncProperty> CustomAsyncProperties { get; }

    /// <summary>
    ///   Gets the observable collection of custom device actions.
    ///   The <see cref="IDeviceAction.TargetDevice" /> properties of device actions being added to the collection will
    ///   be automatically assigned.
    /// </summary>
    ObservableCollection<IDeviceAction> CustomDeviceActions { get; }

    /// <summary>
    ///   Gets or sets the custom device initialization stage callback delegate.
    /// </summary>
    Action<TVisaDevice>? CustomInitializeCallback { get; set; }

    /// <summary>
    ///   Gets or sets the custom device de-initialization stage callback delegate.
    /// </summary>
    Action<TVisaDevice>? CustomDeInitializeCallback { get; set; }

    /// <summary>
    ///   Gets or sets the custom delegate for getting the device identifier string.
    /// </summary>
    Func<TVisaDevice, string>? CustomGetIdentifierCallback { get; set; }

    /// <summary>
    ///   Gets or sets the custom delegate to reset the device.
    /// </summary>
    Action<TVisaDevice>? CustomResetCallback { get; set; }
  }
}
