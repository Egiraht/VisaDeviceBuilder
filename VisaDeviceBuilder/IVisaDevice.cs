using System;
using System.Collections.Generic;
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
    ///   Gets the unaliased VISA resource name of the device.
    /// </summary>
    string ResourceName { get; }

    /// <summary>
    ///   Gets the connection timeout in milliseconds.
    /// </summary>
    int ConnectionTimeout { get; }

    /// <summary>
    ///   Gets the VISA alias name of the device if it is available, otherwise gets its unaliased resource name.
    /// </summary>
    string AliasName { get; }

    /// <summary>
    ///   Gets the VISA hardware interface used for communication with the device.
    /// </summary>
    HardwareInterfaceType Interface { get; }

    /// <summary>
    ///   Gets the array of VISA hardware interfaces supported by the device.
    /// </summary>
    HardwareInterfaceType[] SupportedInterfaces { get; }

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
    ///   Checks if a VISA session has been opened for the device.
    /// </summary>
    bool IsSessionOpened { get; }

    /// <summary>
    ///   Gets the collection of remote properties available for the current device.
    /// </summary>
    ICollection<IRemoteProperty> RemoteProperties { get; }

    /// <summary>
    ///   Asynchronously opens a connection session with the device.
    ///   After opening a new session the <see cref="InitializeAsync" /> method is called.
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
    ///   Before closing the opened session the <see cref="DeInitializeAsync" /> method is called.
    /// </summary>
    Task CloseSessionAsync();
  }
}
