// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for VISA device controller classes.
  /// </summary>
  public interface IVisaDeviceController : IVisaDeviceAccessor, INotifyPropertyChanged, IDisposable, IAsyncDisposable
  {
    /// <summary>
    ///   Checks if the device can be connected at the moment.
    /// </summary>
    bool CanConnect { get; }

    /// <summary>
    ///   Checks if the device has been successfully connected, initialized, and is ready for communication.
    /// </summary>
    bool IsDeviceReady { get; }

    /// <summary>
    ///   Gets the device identifier.
    /// </summary>
    string Identifier { get; }

    /// <summary>
    ///   Gets the auto-updater object that allows to continuously update getters of the device's asynchronous
    ///   properties.
    /// </summary>
    /// <remarks>
    ///   To update the getters once use the <see cref="UpdateAsyncPropertiesAsync" /> method.
    /// </remarks>
    IAutoUpdater AutoUpdater { get; }

    /// <summary>
    ///   Gets or sets the flag defining if the background auto-updater for asynchronous properties should be
    ///   enabled. When enabled, the auto-updater will run only when the device is connected and is ready.
    /// </summary>
    bool IsAutoUpdaterEnabled { get; set; }

    /// <summary>
    ///   Gets or sets the auto-updater delay value in milliseconds that should be awaited between the two consequent
    ///   updates of asynchronous properties.
    /// </summary>
    int AutoUpdaterDelay { get; set; }

    /// <summary>
    ///   The event that is called when the VISA device gets successfully connected to the controller.
    /// </summary>
    event EventHandler? Connected;

    /// <summary>
    ///   The event that is called when the VISA device gets finally disconnected from the controller.
    /// </summary>
    event EventHandler? Disconnected;

    /// <summary>
    ///   The event that is called on any device controller exception.
    /// </summary>
    event ThreadExceptionEventHandler? Exception;

    /// <summary>
    ///   Begins the asynchronous device connection process.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Use the <see cref="GetDeviceConnectionTask" /> method to get the awaitable connection <see cref="Task" />
    ///     object.
    ///   </para>
    ///   <para>
    ///     Use the <see cref="BeginDisconnect" /> method to interrupt the ongoing connection process as well as to
    ///     disconnect the previously connected device.
    ///   </para>
    /// </remarks>
    void BeginConnect();

    /// <summary>
    ///   Gets the awaitable device connection <see cref="Task" /> that encapsulates the entire device connection and
    ///   initialization process.
    /// </summary>
    /// <returns>
    ///   The device connection process <see cref="Task" /> object if there is an ongoing connection process, or a
    ///   <see cref="Task.CompletedTask" /> otherwise.
    /// </returns>
    Task GetDeviceConnectionTask();

    /// <summary>
    ///   Asynchronously interrupts the ongoing connection process or disconnect the previously connected device.
    /// </summary>
    void BeginDisconnect();

    /// <summary>
    ///   Gets the awaitable device disconnection <see cref="Task" /> that encapsulates the entire device
    ///   de-initialization and final disconnection process.
    /// </summary>
    /// <returns>
    ///   The device disconnection process <see cref="Task" /> object if there is an ongoing disconnection process, or a
    ///   <see cref="Task.CompletedTask" /> otherwise.
    /// </returns>
    Task GetDeviceDisconnectionTask();

    /// <summary>
    ///   Asynchronously updates getters of device's asynchronous properties once.
    /// </summary>
    /// <remarks>
    ///   For continuous updates use the <see cref="AutoUpdater" />.
    /// </remarks>
    Task UpdateAsyncPropertiesAsync();
  }
}
