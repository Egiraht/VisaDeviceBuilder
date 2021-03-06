// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows.Input;

namespace VisaDeviceBuilder.WPF.Components
{
  /// <summary>
  ///   The simple <see cref="ICommand" /> implementation for conditional execution of the provided action.
  /// </summary>
  public class RelayCommand : ICommand
  {
    /// <summary>
    ///   The action to be executed when the command is called.
    /// </summary>
    public Action<object?>? Action { get; }

    /// <summary>
    ///   The conditional callback that defines if the action can be executed at the moment.
    /// </summary>
    public Func<object?, bool>? Condition { get; }

    /// <summary>
    ///   Creates a new command instance.
    /// </summary>
    /// <param name="action">
    ///   The action to be executed.
    ///   The provided action can be executed without any conditions.
    /// </param>
    public RelayCommand(Action<object?> action)
    {
      Action = action;
    }

    /// <summary>
    ///   Creates a new command instance.
    /// </summary>
    /// <param name="action">
    ///   The device action to be started.
    ///   The provided action can be executed without any conditions.
    /// </param>
    public RelayCommand(Func<object?, Task> action)
    {
      Action = parameter => action(parameter);
    }

    /// <summary>
    ///   Creates a new command instance.
    /// </summary>
    /// <param name="action">
    ///   The action to be conditionally executed.
    /// </param>
    /// <param name="condition">
    ///   The conditional callback that defines if the action can be executed at the moment.
    /// </param>
    public RelayCommand(Action<object?> action, Func<object?, bool> condition) : this(action)
    {
      Condition = condition;
    }

    /// <summary>
    ///   Creates a new command instance.
    /// </summary>
    /// <param name="action">
    ///   The device action to be conditionally started.
    /// </param>
    /// <param name="condition">
    ///   The conditional callback that defines if the action can be executed at the moment.
    /// </param>
    public RelayCommand(Func<object?, Task> action, Func<object?, bool> condition) : this(action)
    {
      Condition = condition;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter = null)
    {
      try
      {
        return Condition?.Invoke(parameter) ?? true;
      }
      catch
      {
        return false;
      }
    }

    /// <inheritdoc />
    public void Execute(object? parameter = null)
    {
      if (CanExecute(parameter))
        Action?.Invoke(parameter);
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public event EventHandler? CanExecuteChanged
    {
      add => CommandManager.RequerySuggested += value;
      remove => CommandManager.RequerySuggested -= value;
    }
  }
}
