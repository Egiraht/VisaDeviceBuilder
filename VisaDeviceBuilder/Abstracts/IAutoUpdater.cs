// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VisaDeviceBuilder.Abstracts
{
  public interface IAutoUpdater : IDisposable, IAsyncDisposable
  {
    /// <summary>
    ///   Gets the collection of asynchronous properties that are updated by this auto-updater instance.
    /// </summary>
    IEnumerable<IAsyncProperty> AsyncProperties { get; }

    /// <summary>
    ///   Gets or sets the delay value awaited before updating the next available asynchronous property.
    /// </summary>
    TimeSpan Delay { get; set; }

    /// <summary>
    ///   Checks if the auto-updater is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    ///   The event fired when every single loop cycle elapses.
    /// </summary>
    event EventHandler? AutoUpdateCycle;

    /// <summary>
    ///   The event fired when an exception is thrown from within the auto-update loop.
    /// </summary>
    /// <remarks>
    ///   The thrown exception does not stop the loop, so the action will continue get periodically called until it
    ///   will be explicitly stopped using the <see cref="AutoUpdater.Stop" /> method or the object will be disposed.
    /// </remarks>
    event ThreadExceptionEventHandler? AutoUpdateException;

    /// <summary>
    ///   Starts the auto-updater.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    ///   The object was disposed.
    /// </exception>
    void Start();

    /// <summary>
    ///   Stops the auto-updater and waits until its auto-update loop finally stops.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    ///   The object was disposed.
    /// </exception>
    void Stop();

    /// <summary>
    ///   Asynchronously stops the auto-updater.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    ///   The object was disposed.
    /// </exception>
    /// <returns>
    ///   The <see cref="Task" /> object that completes when the auto-update loop finally stops.
    /// </returns>
    Task StopAsync();
  }
}
