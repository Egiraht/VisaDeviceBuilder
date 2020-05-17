using System;
using System.Threading.Tasks;
using Ivi.Visa;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The interface describing a connectable VISA device.
  /// </summary>
  public interface IVisaDevice : IDisposable, IAsyncDisposable
  {
    /// <summary>
    ///   Gets the current device connection state.
    /// </summary>
    DeviceConnectionState DeviceConnectionState { get; }

    /// <summary>
    ///   Gets the current VISA session object if the connection has been successfully established,
    ///   or <c>null</c> otherwise.
    /// </summary>
    IVisaSession? Session { get; }

    /// <summary>
    ///   Gets the VISA resource name of the device.
    /// </summary>
    string ResourceName { get; }

    /// <summary>
    ///   Gets the VISA alias name of the device if it is available.
    /// </summary>
    string AliasName { get; }

    /// <summary>
    ///   Gets the VISA hardware interface used for communication with the device.
    /// </summary>
    HardwareInterfaceType Interface { get; }

    /// <summary>
    ///   Gets the array of VISA hardware interfaces supported by the device.
    /// </summary>
    public HardwareInterfaceType[] SupportedInterfaces { get; }

    /// <summary>
    ///   Asynchronously opens a connection session with the device.
    /// </summary>
    Task OpenSessionAsync();

    /// <summary>
    ///   Asynchronously initializes the device after the successful session opening.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    ///   Asynchronously reads the device identifier string.
    /// </summary>
    /// <returns>
    ///   A string containing the identifier of the connected device.
    /// </returns>
    Task<string> GetIdentifierAsync();

    /// <summary>
    ///   Asynchronously resets the device to some predefined state.
    /// </summary>
    Task ResetAsync();

    /// <summary>
    ///   Asynchronously de-initializes the device before the session closing.
    /// </summary>
    Task DeInitializeAsync();

    /// <summary>
    ///   Asynchronously closes the connection session with the device.
    /// </summary>
    /// <returns></returns>
    Task CloseSessionAsync();
  }
}
