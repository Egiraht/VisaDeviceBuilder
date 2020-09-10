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
    ///   Gets the custom VISA resource manager instance used for VISA session management.
    ///   If set to <c>null</c>, the <see cref="GlobalResourceManager" /> static class will be used.
    /// </summary>
    IResourceManager? ResourceManager { get; }

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
    ///   Gets the dictionary of asynchronous properties available for the current device.
    ///   Keys of the dictionary contain the names of corresponding asynchronous properties stored as values.
    /// </summary>
    IDictionary<string, IAsyncProperty> AsyncProperties { get; }

    /// <summary>
    ///   Gets the dictionary of device actions available for the current device.
    ///   Keys of the dictionary contain the names of corresponding device actions stored as values.
    /// </summary>
    IDictionary<string, Action> DeviceActions { get; }

    /// <summary>
    ///   Synchronously opens a connection session with the device.
    /// </summary>
    void OpenSession();

    /// <summary>
    ///   Asynchronously opens a connection session with the device.
    /// </summary>
    Task OpenSessionAsync();

    /// <summary>
    ///   Reads the device identifier string.
    /// </summary>
    /// <returns>
    ///   A string containing the identifier of the connected device.
    /// </returns>
    string GetIdentifier();

    /// <summary>
    ///   Asynchronously reads the device identifier string.
    /// </summary>
    /// <returns>
    ///   A string containing the identifier of the connected device.
    /// </returns>
    Task<string> GetIdentifierAsync();

    /// <summary>
    ///   Resets the device to some predefined state.
    /// </summary>
    void Reset();

    /// <summary>
    ///   Asynchronously resets the device to some predefined state.
    /// </summary>
    Task ResetAsync();

    /// <summary>
    ///   Closes the connection session with the device.
    /// </summary>
    void CloseSession();

    /// <summary>
    ///   Asynchronously closes the connection session with the device.
    /// </summary>
    Task CloseSessionAsync();
  }
}
