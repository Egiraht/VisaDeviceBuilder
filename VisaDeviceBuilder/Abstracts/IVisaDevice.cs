// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ivi.Visa;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The interface describing a connectable VISA device.
  /// </summary>
  public interface IVisaDevice : ICloneable, IDisposable, IAsyncDisposable
  {
    /// <summary>
    ///   Gets or sets a custom VISA resource manager for VISA session management.
    ///   If set to <c>null</c>, the <see cref="GlobalResourceManager" /> static class will be used.
    /// </summary>
    /// <remarks>
    ///   When using the <see cref="GlobalResourceManager" /> class for VISA resource management with the
    ///   <i>.NET Core</i> runtime, the assembly <i>.dll</i> files of installed VISA .NET implementations must be
    ///   directly referenced in the project. This is because the <i>.NET Core</i> runtime does not automatically
    ///   locate assemblies from the system's Global Assembly Cache (GAC) used by the <i>.NET Framework</i> runtime,
    ///   where the VISA standard prescribes to install the VISA .NET implementation libraries.
    /// </remarks>
    IResourceManager? ResourceManager { get; set; }

    /// <summary>
    ///   Gets or sets the VISA resource name of the device.
    /// </summary>
    /// <seealso cref="VisaResourceLocator" />
    string ResourceName { get; set; }

    /// <summary>
    ///   Gets the VISA resource information parsed from the <see cref="ResourceName" /> value.
    /// </summary>
    /// <returns>
    ///   A <see cref="ParseResult" /> object containing the current VISA resource information, or <c>null</c> if
    ///   <see cref="ResourceName" /> value parsing fails.
    /// </returns>
    ParseResult? ResourceNameInfo { get; }

    /// <summary>
    ///   Gets the VISA alias name of the device if it is available, otherwise gets its unaliased resource name.
    /// </summary>
    string AliasName { get; }

    /// <summary>
    ///   Gets or sets the connection timeout in milliseconds.
    /// </summary>
    int ConnectionTimeout { get; set; }

    /// <summary>
    ///   Gets the array of VISA hardware interfaces supported by the device.
    /// </summary>
    HardwareInterfaceType[] SupportedInterfaces { get; }

    /// <summary>
    ///   Gets the current device connection state.
    /// </summary>
    DeviceConnectionState ConnectionState { get; }

    /// <summary>
    ///   Gets the current VISA session object.
    /// </summary>
    /// <returns>
    ///   The current VISA session object if the VISA device has been successfully connected, otherwise <c>null</c>.
    /// </returns>
    IVisaSession? Session { get; }

    /// <summary>
    ///   Checks if a VISA session has been opened for the device.
    /// </summary>
    bool IsSessionOpened { get; }

    /// <summary>
    ///   Gets the enumeration of asynchronous properties defined in the current device.
    /// </summary>
    IEnumerable<IAsyncProperty> AsyncProperties { get; }

    /// <summary>
    ///   Gets the enumeration of device actions defined in the current device.
    /// </summary>
    IEnumerable<IDeviceAction> DeviceActions { get; }

    /// <summary>
    ///   The event that is called every time the connection state of the device changes.
    /// </summary>
    event EventHandler<DeviceConnectionState>? ConnectionStateChanged;

    /// <summary>
    ///   Synchronously opens a connection session with the device.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    ///   The device instance has been already disposed of.
    /// </exception>
    /// <exception cref="VisaDeviceException">
    ///   An error has occured during session opening with the device. See the <see cref="Exception.InnerException" />
    ///   for more information.
    /// </exception>
    void OpenSession();

    /// <summary>
    ///   Asynchronously opens a connection session with the device.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    ///   The device instance has been already disposed of.
    /// </exception>
    /// <exception cref="VisaDeviceException">
    ///   An error has occured during session opening with the device. See the <see cref="Exception.InnerException" />
    ///   for more information.
    /// </exception>
    Task OpenSessionAsync();

    /// <summary>
    ///   Reads the device identifier string.
    /// </summary>
    /// <returns>
    ///   A string containing the identifier of the connected device.
    /// </returns>
    /// <exception cref="VisaDeviceException">
    ///   There is no opened VISA session to perform an operation.
    /// </exception>
    string GetIdentifier();

    /// <summary>
    ///   Asynchronously reads the device identifier string.
    /// </summary>
    /// <returns>
    ///   A string containing the identifier of the connected device.
    /// </returns>
    /// <exception cref="VisaDeviceException">
    ///   There is no opened VISA session to perform an operation.
    /// </exception>
    Task<string> GetIdentifierAsync();

    /// <summary>
    ///   Resets the device to some predefined state.
    /// </summary>
    /// <exception cref="VisaDeviceException">
    ///   There is no opened VISA session to perform an operation.
    /// </exception>
    void Reset();

    /// <summary>
    ///   Asynchronously resets the device to some predefined state.
    /// </summary>
    /// <exception cref="VisaDeviceException">
    ///   There is no opened VISA session to perform an operation.
    /// </exception>
    Task ResetAsync();

    /// <summary>
    ///   Closes the connection session with the device.
    /// </summary>
    void CloseSession();

    /// <summary>
    ///   Asynchronously closes the connection session with the device.
    /// </summary>
    Task CloseSessionAsync();

    /// <summary>
    ///   Closes the connection session with the device.
    ///   It is similar to the <see cref="CloseSession" /> method but it also sets the <see cref="ConnectionState" />
    ///   property to <see cref="DeviceConnectionState.DisconnectedWithError" /> after the device gets disconnected.
    /// </summary>
    void CloseSessionWithError();

    /// <summary>
    ///   Asynchronously closes the connection session with the device.
    ///   It is similar to the <see cref="CloseSessionAsync" /> method but it also sets the
    ///   <see cref="ConnectionState" /> property to <see cref="DeviceConnectionState.DisconnectedWithError" /> after
    ///   the device gets disconnected.
    /// </summary>
    Task CloseSessionWithErrorAsync();
  }
}
