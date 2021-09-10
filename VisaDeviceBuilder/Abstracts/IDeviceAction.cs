// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Threading;
using System.Threading.Tasks;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for device action classes.
  /// </summary>
  public interface IDeviceAction : ICloneable
  {
    /// <summary>
    ///   Gets or sets the name of the device action.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    ///   The VISA device instance this device action currently targets.
    ///   This reference will be passed to the device action delegate when it is executed, so ensure it is set
    ///   correctly before any execution occurs.
    ///   May be set to <c>null</c>, if the device action does not require a VISA device instance for functioning.
    /// </summary>
    IVisaDevice? TargetDevice { get; set; }

    /// <summary>
    ///   Gets the actual device action delegate this object wraps.
    ///   The delegate may accept a nullable VISA device instance from the <see cref="TargetDevice" /> property as a
    ///   parameter, or just reject it if it is not required for functioning.
    /// </summary>
    Action<IVisaDevice?> DeviceActionDelegate { get; }

    /// <summary>
    ///   Checks if the current device action can be executed at the moment.
    /// </summary>
    bool CanExecute { get; }

    /// <summary>
    ///   The event that is called when an exception occurs during this device action processing.
    ///   The event arguments object contains the exception instance that has raised the event.
    /// </summary>
    event ThreadExceptionEventHandler? Exception;

    /// <summary>
    ///   The event called when this device action execution completes.
    /// </summary>
    event EventHandler? ExecutionCompleted;

    /// <summary>
    ///   Asynchronously executes the current device action with its state being tracked by the
    ///   <see cref="DeviceActionExecutor" /> static class.
    /// </summary>
    Task ExecuteAsync();
  }
}
