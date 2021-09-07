using System;
using System.Collections.ObjectModel;
using Ivi.Visa;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for VISA devices that can be built using builders.
  /// </summary>
  public interface IBuildableVisaDevice : IVisaDevice
  {
    /// <summary>
    ///   Gets the array of custom hardware interfaces supported by the device.
    ///   If set to <c>null</c>, the default hardware interfaces defined for the VISA device will be supported.
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
    Action<IVisaDevice?>? CustomInitializeCallback { get; set; }

    /// <summary>
    ///   Gets or sets the custom device de-initialization stage callback delegate.
    /// </summary>
    Action<IVisaDevice?>? CustomDeInitializeCallback { get; set; }

    /// <summary>
    ///   Gets or sets the custom delegate for getting the device identifier string.
    /// </summary>
    Func<IVisaDevice?, string>? CustomGetIdentifierCallback { get; set; }

    /// <summary>
    ///   Gets or sets the custom delegate to reset the device.
    /// </summary>
    Action<IVisaDevice?>? CustomResetCallback { get; set; }
  }
}
