using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using Ivi.Visa;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for VISA device controller classes.
  /// </summary>
  public interface IVisaDeviceController : INotifyPropertyChanged, IDisposable, IAsyncDisposable
  {
    /// <summary>
    ///   Gets the VISA device object this controller instance is created for.
    /// </summary>
    IVisaDevice Device { get; }

    /// <summary>
    ///   Checks if the device is a message device (its type implements the <see cref="IMessageDevice" /> interface).
    /// </summary>
    bool IsMessageDevice { get; }

    /// <inheritdoc cref="IVisaDevice.ResourceManager" />
    public IResourceManager? ResourceManager { get; set; }

    /// <inheritdoc cref="IVisaDevice.ResourceName" />
    string ResourceName { get; set; }

    /// <summary>
    ///   Gets the read-only collection of asynchronous properties defined for the device.
    /// </summary>
    ReadOnlyObservableCollection<IAsyncProperty> AsyncProperties { get; }

    /// <summary>
    ///   Gets the read-only collection of device actions defined for the device.
    /// </summary>
    ReadOnlyObservableCollection<IDeviceAction> DeviceActions { get; }

    /// <summary>
    ///   Gets the collection of available VISA resources. The collection may contain both canonical VISA resource
    ///   names and corresponding alias names if they are available.
    /// </summary>
    ReadOnlyObservableCollection<string> AvailableVisaResources { get; }

    /// <summary>
    ///   Checks if the <see cref="AvailableVisaResources" /> property is being updated.
    /// </summary>
    bool IsUpdatingVisaResources { get; }

    /// <summary>
    ///   Checks if the device with the specified <see cref="ResourceName" /> can be connected at the moment.
    /// </summary>
    bool CanConnect { get; }

    /// <summary>
    ///   Checks if the device with the specified <see cref="ResourceName" /> is successfully connected, initialized,
    ///   and ready for communication.
    /// </summary>
    bool IsDeviceReady { get; }

    /// <summary>
    ///   Gets the device identifier.
    /// </summary>
    string Identifier { get; }

    /// <summary>
    ///   Gets or sets the optional ResX resource manager instance used for localization of the names of available
    ///   asynchronous properties and actions.
    ///   The provided localization resource manager must be able to accept the original names of the asynchronous
    ///   properties and actions and return their localized names.
    ///   If not provided, the original names will be used without localization.
    /// </summary>
    ResourceManager? LocalizationResourceManager { get; set; }

    /// <summary>
    ///   Checks if the asynchronous properties are being updated at the moment after calling the
    ///   <see cref="UpdateAsyncPropertiesAsync"/> method.
    /// </summary>
    bool IsUpdatingAsyncProperties { get; }

    /// <summary>
    ///   Gets the auto-updater object that allows to automatically update getters of asynchronous properties
    ///   available in the <see cref="AsyncProperties" /> collection.
    /// </summary>
    IAutoUpdater AutoUpdater { get; }

    /// <summary>
    ///   Gets or sets the flag controlling if the background auto-updater for asynchronous properties should be
    ///   enabled. When enabled, the auto-updater works only when the device is connected and is ready.
    /// </summary>
    bool IsAutoUpdaterEnabled { get; set; }

    /// <summary>
    ///   Gets or sets the auto-updater delay value in milliseconds that should be awaited between the two consequent
    ///   updates of asynchronous properties.
    /// </summary>
    int AutoUpdaterDelay { get; set; }

    /// <summary>
    ///   Checks if the device disconnection has been requested using the <see cref="DisconnectAsync" /> method.
    /// </summary>
    bool IsDisconnectionRequested { get; }

    /// <summary>
    ///   The event that is called on any device controller exception caught during the connection session.
    /// </summary>
    event ThreadExceptionEventHandler? Exception;

    /// <summary>
    ///   Asynchronously updates the list of VISA resource names.
    /// </summary>
    Task UpdateResourcesListAsync();

    /// <summary>
    ///   Starts the asynchronous device connection process.
    /// </summary>
    void Connect();

    /// <summary>
    ///   Stops the device connection loop.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    ///   Asynchronously updates getters of all asynchronous properties registered in the <see cref="AsyncProperties" />
    ///   collection.
    /// </summary>
    Task UpdateAsyncPropertiesAsync();
  }
}
