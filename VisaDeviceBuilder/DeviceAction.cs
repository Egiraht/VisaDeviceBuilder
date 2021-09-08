// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The class representing an action that can be asynchronously executed by a VISA device.
  ///   The special <see cref="DeviceActionExecutor" /> static class can help to track execution states of device
  ///   actions.
  /// </summary>
  public class DeviceAction : IDeviceAction
  {
    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public IVisaDevice? TargetDevice { get; set; }

    /// <inheritdoc />
    public virtual Action<IVisaDevice?> DeviceActionDelegate { get; }

    /// <inheritdoc />
    public bool CanExecute => DeviceActionExecutor.CanExecute(this);

    /// <summary>
    ///   Creates a new device action instance.
    /// </summary>
    /// <param name="deviceActionDelegate">
    ///   The action delegate representing a device action.
    ///   The delegate may accept a nullable VISA device instance from the <see cref="TargetDevice" /> property as a
    ///   parameter, or just reject it if it is not required for functioning.
    /// </param>
    public DeviceAction(Action<IVisaDevice?> deviceActionDelegate)
    {
      DeviceActionDelegate = deviceActionDelegate;

      DeviceActionExecutor.Exception += OnException;
      DeviceActionExecutor.DeviceActionCompleted += OnExecutionCompleted;
    }

    /// <inheritdoc />
    /// <remarks>
    ///   This event handles the
    ///   <see cref="DeviceActionExecutor" />.<see cref="DeviceActionExecutor.Exception"/> global event.
    /// </remarks>
    public event ThreadExceptionEventHandler? Exception;

    /// <inheritdoc />
    /// <remarks>
    ///   This event handles the
    ///   <see cref="DeviceActionExecutor" />.<see cref="DeviceActionExecutor.DeviceActionCompleted"/> global event.
    /// </remarks>
    public event EventHandler? ExecutionCompleted;

    /// <inheritdoc />
    public async Task ExecuteAsync()
    {
      DeviceActionExecutor.BeginExecute(this);
      await DeviceActionExecutor.GetDeviceActionTask(this);
    }

    /// <summary>
    ///   Handles the <see cref="DeviceActionExecutor" />.<see cref="DeviceActionExecutor.Exception"/> event.
    /// </summary>
    /// <param name="sender">
    ///   The event sender object.
    /// </param>
    /// <param name="args">
    ///   The event arguments object.
    /// </param>
    private void OnException(object sender, ThreadExceptionEventArgs args)
    {
      if (sender == this)
        Exception?.Invoke(sender, args);
    }

    /// <summary>
    ///   Handles the <see cref="DeviceActionExecutor" />.<see cref="DeviceActionExecutor.DeviceActionCompleted"/>
    ///   event.
    /// </summary>
    /// <param name="sender">
    ///   The event sender object.
    /// </param>
    /// <param name="args">
    ///   The event arguments object.
    /// </param>
    private void OnExecutionCompleted(object? sender, EventArgs args)
    {
      if (sender == this)
        ExecutionCompleted?.Invoke(sender, EventArgs.Empty);
    }

    /// <inheritdoc />
    public virtual object Clone() => new DeviceAction(DeviceActionDelegate)
    {
      Name = Name,
      TargetDevice = TargetDevice
    };

    /// <summary>
    ///   Implicitly converts the provided device action instance to its action delegate.
    /// </summary>
    /// <param name="action">
    ///   The device action instance to convert.
    /// </param>
    /// <returns>
    ///   The getter value string stored in the provided asynchronous property instance.
    /// </returns>
    [ExcludeFromCodeCoverage]
    public static implicit operator Action<IVisaDevice?>(DeviceAction action) => action.DeviceActionDelegate;
  }
}
