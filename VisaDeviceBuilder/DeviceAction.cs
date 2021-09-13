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
    public Func<IVisaDevice?, bool> CanExecuteDelegate { get; } = _ => true;

    /// <inheritdoc />
    public bool IsExecuting => ExecutionTask != null;

    /// <inheritdoc />
    public bool CanExecute => !IsExecuting && CanExecuteDelegate.Invoke(TargetDevice);

    /// <summary>
    ///   Gets or sets the <see cref="Task" /> of the current device action execution process.
    /// </summary>
    private Task? ExecutionTask { get; set; }

    /// <summary>
    ///   Gets the shared synchronization lock object.
    /// </summary>
    private object SynchronizationLock { get; } = new();

    /// <summary>
    ///   Creates a new device action instance using the provided device action delegate.
    /// </summary>
    /// <param name="deviceActionDelegate">
    ///   The action delegate representing a device action.
    ///   The delegate may accept a nullable VISA device instance from the <see cref="TargetDevice" /> property as a
    ///   parameter, or just reject it if it is not required for functioning.
    /// </param>
    public DeviceAction(Action<IVisaDevice?> deviceActionDelegate) => DeviceActionDelegate = deviceActionDelegate;

    /// <summary>
    ///   Creates a new device action instance using the provided device action delegate and the custom delegate
    ///   determining if it can be executed at the moment.
    /// </summary>
    /// <param name="deviceActionDelegate">
    ///   The delegate representing a device action.
    ///   The delegate may accept a nullable VISA device instance from the <see cref="TargetDevice" /> property as a
    ///   parameter, or just reject it if it is not required for functioning.
    /// </param>
    /// <param name="canExecuteDelegate">
    ///   The custom delegate that checks if the device action can be executed at the moment.
    ///   The delegate may accept a nullable VISA device instance from the <see cref="TargetDevice" /> property as a
    ///   parameter, or just reject it if it is not required for functioning. It also must return a boolean value
    ///   determining if the device action can be executed.
    /// </param>
    public DeviceAction(Action<IVisaDevice?> deviceActionDelegate, Func<IVisaDevice?, bool> canExecuteDelegate) :
      this(deviceActionDelegate) => CanExecuteDelegate = canExecuteDelegate;

    /// <inheritdoc />
    public event ThreadExceptionEventHandler? Exception;

    /// <inheritdoc />
    public event EventHandler? ExecutionCompleted;

    /// <inheritdoc />
    public Task ExecuteAsync()
    {
      if (!CanExecute)
        return Task.CompletedTask;

      lock (SynchronizationLock)
      {
        return ExecutionTask = Task.Run(() =>
        {
          try
          {
            DeviceActionDelegate.Invoke(TargetDevice);
          }
          catch (Exception e)
          {
            OnException(e);
          }
          finally
          {
            ExecutionTask = null;
            OnExecutionCompleted();
          }
        });
      }
    }

    /// <inheritdoc />
    public Task GetExecutionTask() => ExecutionTask ?? Task.CompletedTask;

    /// <summary>
    ///   Invokes the <see cref="Exception" /> event.
    /// </summary>
    /// <param name="exception">
    ///   The exception instance.
    /// </param>
    private void OnException(Exception exception) => Exception?.Invoke(this, new ThreadExceptionEventArgs(exception));

    /// <summary>
    ///   Invokes the <see cref="ExecutionCompleted" /> event.
    /// </summary>
    private void OnExecutionCompleted() => ExecutionCompleted?.Invoke(this, EventArgs.Empty);

    /// <inheritdoc />
    public virtual object Clone() => new DeviceAction(DeviceActionDelegate, CanExecuteDelegate)
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
