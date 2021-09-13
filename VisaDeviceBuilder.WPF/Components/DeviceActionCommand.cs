// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder.WPF.Components
{
  /// <summary>
  ///   The singleton class to be used with WPF controls that allows to execute device actions as control commands.
  ///   Its <see cref="Instance" /> static property can be used as a binding source for the
  ///   <see cref="ICommandSource.Command" /> dependency property in any control implementing the
  ///   <see cref="ICommandSource" /> interface. In this case the asynchronous action to be executed must be provided
  ///   to the control's <see cref="ICommandSource.CommandParameter" /> dependency property.
  /// </summary>
  public class DeviceActionCommand : ICommand
  {
    /// <summary>
    ///   The private singleton class instance accessible via the <see cref="Instance" /> static property.
    /// </summary>
    private static ICommand? _instance;

    /// <summary>
    ///   Gets the synchronization context of the owning thread.
    /// </summary>
    protected SynchronizationContext? SynchronizationContext { get; }

    /// <summary>
    ///   Gets the singleton <see cref="ICommand" /> reference of the class. It can be used as a binding source for the
    ///   <see cref="ICommandSource.Command" /> dependency property in any control implementing the
    ///   <see cref="ICommandSource" /> interface. In this case the asynchronous action to be executed must be provided
    ///   to the control's <see cref="ICommandSource.CommandParameter" /> dependency property.
    /// </summary>
    public static ICommand Instance => _instance ??= new DeviceActionCommand();

    /// <summary>
    ///   Creates a singleton class instance.
    /// </summary>
    protected DeviceActionCommand() => SynchronizationContext = SynchronizationContext.Current;

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    ///   Checks if the device action provided as a <paramref name="parameter" /> can be executed at the moment.
    /// </summary>
    /// <param name="parameter">
    ///   The device action to check the state for. It must be a delegate of <see cref="Action" /> type (synchronous
    ///   action) or of <see cref="Func{TResult}" /> type returning a <see cref="Task" /> (asynchronous action).
    /// </param>
    /// <returns>
    ///   <c>true</c> if the provided device action can be executed at the moment, or <c>false</c> otherwise.
    ///   <c>false</c> is also returned if the provided <paramref name="parameter" /> value is not a device action.
    /// </returns>
    public bool CanExecute(object? parameter) => parameter is IDeviceAction { CanExecute: true };

    /// <summary>
    ///   Starts asynchronous execution of the device action provided as a <paramref name="parameter" /> if it is
    ///   a valid device action type and can be executed at the moment.
    /// </summary>
    /// <param name="parameter">
    ///   The device action to execute. It must be a delegate of <see cref="Action" /> type (synchronous action)
    ///   or of <see cref="Func{TResult}" /> type returning a <see cref="Task" /> (asynchronous action).
    /// </param>
    public void Execute(object? parameter)
    {
      if (parameter is not IDeviceAction { CanExecute: true } deviceAction)
        return;

      deviceAction.ExecuteAsync().ContinueWith(_ => OnCanExecuteChanged());
      OnCanExecuteChanged();
    }

    /// <summary>
    ///   Invokes the <see cref="CanExecuteChanged" /> event.
    /// </summary>
    protected virtual void OnCanExecuteChanged()
    {
      if (SynchronizationContext != null)
        SynchronizationContext.Post(_ => CanExecuteChanged?.Invoke(this, EventArgs.Empty), null);
      else
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
  }
}
