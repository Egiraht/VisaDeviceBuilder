using System;
using System.Collections.Generic;
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
    /// </summary>
    HardwareInterfaceType[]? CustomSupportedInterfaces { get; set; }

    /// <summary>
    ///   Gets the observable collection of custom owned asynchronous properties.
    ///   The buildable device instance automatically takes ownership of owned asynchronous properties added to the
    ///   collection.
    /// </summary>
    ObservableCollection<IOwnedAsyncProperty<TVisaDevice>> CustomAsyncProperties { get; }

    /// <summary>
    ///   Gets the observable collection of custom owned device actions.
    ///   The buildable device instance automatically takes ownership of owned device actions added to the collection.
    /// </summary>
    ObservableCollection<IOwnedDeviceAction<TVisaDevice>> CustomDeviceActions { get; }

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

    /// <summary>
    ///   Gets the list of custom disposable objects.
    /// </summary>
    List<IDisposable> CustomDisposables { get; }
  }
}
