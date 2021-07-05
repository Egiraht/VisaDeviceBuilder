using System;
using System.Diagnostics.CodeAnalysis;
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
  ///   The class logic is based on the <see cref="DeviceActionExecutor" /> static class.
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
    protected DeviceActionCommand()
    {
      SynchronizationContext = SynchronizationContext.Current;
      DeviceActionExecutor.DeviceActionCompleted += OnDeviceActionCompleted;
    }

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
    ///   <c>true</c> if the provided device action can be executed at the moment, and <c>false</c> if the provided
    ///   device action is already being executed at the moment and cannot be executed repeatedly until it finishes,
    ///   or the provided <paramref name="parameter" /> value is not a valid device action type.
    /// </returns>
    public bool CanExecute(object? parameter) =>
      parameter is IDeviceAction deviceAction && DeviceActionExecutor.CanExecute(deviceAction);

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
      if (parameter is not IDeviceAction deviceAction)
        return;

      DeviceActionExecutor.BeginExecute(deviceAction);
      OnCanExecuteChanged();
    }

    /// <summary>
    ///   Invokes the <see cref="CanExecuteChanged" /> event.
    /// </summary>
    protected virtual void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    ///   Handles the <see cref="DeviceActionExecutor" />.<see cref="DeviceActionExecutor.DeviceActionCompleted" />
    ///   event.
    /// </summary>
    /// <param name="sender">
    ///   The device action delegate that has raised the event.
    /// </param>
    /// <param name="e">
    ///   The event arguments object.
    /// </param>
    [ExcludeFromCodeCoverage]
    protected virtual void OnDeviceActionCompleted(object? sender, EventArgs e)
    {
      if (SynchronizationContext != null)
        SynchronizationContext.Post(_ => OnCanExecuteChanged(), null);
      else
        OnCanExecuteChanged();
    }
  }
}
