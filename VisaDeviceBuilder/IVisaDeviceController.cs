using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using Ivi.Visa;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The common interface for VISA device controller classes.
  /// </summary>
  public interface IVisaDeviceController : INotifyPropertyChanged, IDisposable, IAsyncDisposable
  {
    /// <summary>
    ///   Gets or sets the type of the device.
    ///   The device class defined by the specified type must implement the <see cref="IVisaDevice" /> interface.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///   The provided type value does not implement the <see cref="IVisaDevice" /> interface.
    /// </exception>
    Type DeviceType { get; set; }

    /// <summary>
    ///   Gets or sets the type of the VISA resource manager.
    ///   The resource manager class defined by the specified type must implement the <see cref="IResourceManager" />
    ///   interface, or the value can be <c>null</c>.
    ///   If set to <c>null</c>, the default <see cref="GlobalResourceManager" /> static class will be used.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///   The provided type does not implement the <see cref="IResourceManager" /> interface.
    /// </exception>
    Type? VisaResourceManagerType { get; set; }

    /// <summary>
    ///   Gets or sets the VISA resource name used for VISA device location and connection.
    /// </summary>
    string ResourceName { get; set; }

    /// <summary>
    ///   Gets the collection of available VISA resources.
    /// </summary>
    ObservableCollection<string> AvailableVisaResources { get; }

    /// <summary>
    ///   Checks if the <see cref="AvailableVisaResources" /> property is being updated.
    /// </summary>
    bool IsUpdatingVisaResources { get; }

    /// <summary>
    ///   Gets the current VISA device instance of <typeparamref cref="DeviceType" /> type created for the current
    ///   connection.
    /// </summary>
    /// <returns>
    ///   The <see cref="IVisaDevice" /> instance created for the opened device connection or <c>null</c> if no
    ///   device connection is established at the moment.
    /// </returns>
    IVisaDevice? Device { get; }

    /// <summary>
    ///   Checks if the <see cref="Device" /> is not <c>null</c> and its type implements <see cref="IMessageDevice" />.
    /// </summary>
    bool IsMessageDevice { get; }

    /// <summary>
    ///   Gets the current device connection state.
    /// </summary>
    DeviceConnectionState ConnectionState { get; }

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
    ///   Gets the collection of asynchronous properties and corresponding metadata defined for the device.
    /// </summary>
    ObservableCollection<AsyncPropertyMetadata> AsyncProperties { get; }

    /// <summary>
    ///   Gets the collection of device actions and corresponding metadata defined for the device.
    /// </summary>
    ObservableCollection<DeviceActionMetadata> DeviceActions { get; }

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
    ///   Gets or sets the flag controlling if the background auto-updater for asynchronous properties should be
    ///   enabled.
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
    ///   Event that is called on any control exception.
    /// </summary>
    event ThreadExceptionEventHandler? Exception;

    /// <summary>
    ///   Asynchronously updates the list of VISA resource names.
    /// </summary>
    Task UpdateResourcesListAsync();

    /// <summary>
    ///   Starts the asynchronous device connection process.
    ///   The created device connection task can be accessed via the <see cref="VisaDeviceController.ConnectionTask" /> property.
    /// </summary>
    void Connect();

    /// <summary>
    ///   Stops the device connection loop.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    ///   Asynchronously updates getters of all asynchronous properties available in the attached <see cref="VisaDeviceController.Device" />
    ///   instance.
    /// </summary>
    Task UpdateAsyncPropertiesAsync();
  }
}
