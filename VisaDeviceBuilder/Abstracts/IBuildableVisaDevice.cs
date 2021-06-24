using System;
using System.Collections.Generic;
using Ivi.Visa;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for builders of VISA devices.
  /// </summary>
  public interface IBuildableVisaDevice : IVisaDevice
  {
    /// <summary>
    ///   Gets the list of custom hardware interfaces supported by the device.
    /// </summary>
    List<HardwareInterfaceType> CustomSupportedInterfaces { get; }

    /// <summary>
    ///   Gets the list of custom asynchronous properties of the device.
    /// </summary>
    List<IAsyncProperty> CustomAsyncProperties { get; }

    /// <summary>
    ///   Gets the list of custom device actions of the device.
    /// </summary>
    List<IDeviceAction> CustomDeviceActions { get; }

    /// <summary>
    ///   Gets or sets the custom device initialization stage callback delegate.
    /// </summary>
    Action<IVisaDevice>? CustomInitializeCallback { get; set; }

    /// <summary>
    ///   Gets or sets the custom device de-initialization stage callback delegate.
    /// </summary>
    Action<IVisaDevice>? CustomDeInitializeCallback { get; set; }

    /// <summary>
    ///   Gets or sets the custom delegate for getting the device identifier string.
    /// </summary>
    Func<IVisaDevice, string>? CustomGetIdentifierCallback { get; set; }

    /// <summary>
    ///   Gets or sets the custom delegate to reset the device.
    /// </summary>
    Action<IVisaDevice>? CustomResetCallback { get; set; }

    /// <summary>
    ///   Gets the list of custom disposable objects.
    /// </summary>
    List<IDisposable> CustomDisposables { get; }
  }
}
