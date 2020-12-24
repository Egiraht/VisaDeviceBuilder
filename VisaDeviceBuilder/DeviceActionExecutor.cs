using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The static class that executes device actions without duplication and tracks their completion states.
  /// </summary>
  public static class DeviceActionExecutor
  {
    /// <summary>
    ///   Gets the common dictionary that tracks the states of all started device action tasks.
    /// </summary>
    private static Dictionary<Delegate, Task> DeviceActionTaskTracker { get; } = new();

    /// <summary>
    ///   Checks if no device actions are running at the moment.
    /// </summary>
    public static bool NoDeviceActionsAreRunning =>
      !DeviceActionTaskTracker.Any() || DeviceActionTaskTracker.All(pair => pair.Value.IsCompleted);

    /// <summary>
    ///   The event called when an exception is thrown during device action processing.
    ///   The event sender object contains the device action delegate that has raised the event.
    ///   The event arguments object contains the exception instance that has raised the event.
    /// </summary>
    public static event ThreadExceptionEventHandler? Exception;

    /// <summary>
    ///   The event called when any of running device actions completes.
    ///   The event sender object contains the device action delegate that has raised the event.
    /// </summary>
    public static event EventHandler? DeviceActionCompleted;

    /// <summary>
    ///   Checks if the provided device action can be executed at the moment.
    /// </summary>
    /// <param name="deviceAction">
    ///   The device action delegate to check.
    /// </param>
    /// <returns>
    ///   <c>true</c> if the provided device action can be executed at the moment, otherwise <c>false</c>.
    /// </returns>
    public static bool CanExecute(Action deviceAction) => GetDeviceActionTask(deviceAction).IsCompleted;

    /// <summary>
    ///   Checks if the provided device action can be executed at the moment.
    /// </summary>
    /// <param name="deviceAction">
    ///   The asynchronous device action delegate to check.
    /// </param>
    /// <returns>
    ///   <c>true</c> if the provided device action can be executed at the moment, otherwise <c>false</c>.
    /// </returns>
    public static bool CanExecute(Func<Task> deviceAction) => GetDeviceActionTask(deviceAction).IsCompleted;

    /// <summary>
    ///   Starts asynchronous execution of the provided device action if it can be executed at the moment.
    /// </summary>
    /// <param name="deviceAction">
    ///   The device action delegate to execute.
    /// </param>
    public static void Execute(Action deviceAction)
    {
      if (!CanExecute(deviceAction))
        return;

      DeviceActionTaskTracker[deviceAction] = Task.Run(() =>
      {
        try
        {
          deviceAction();
        }
        catch (Exception e)
        {
          Exception?.Invoke(deviceAction, new ThreadExceptionEventArgs(e));
        }
        finally
        {
          DeviceActionTaskTracker.Remove(deviceAction);
          DeviceActionCompleted?.Invoke(deviceAction, EventArgs.Empty);
        }
      });
    }

    /// <summary>
    ///   Starts asynchronous execution of the provided device action if it can be executed at the moment.
    /// </summary>
    /// <param name="asyncDeviceAction">
    ///   The asynchronous device action delegate to execute.
    /// </param>
    public static void Execute(Func<Task> asyncDeviceAction)
    {
      if (!CanExecute(asyncDeviceAction))
        return;

      DeviceActionTaskTracker[asyncDeviceAction] = Task.Run(async () =>
      {
        try
        {
          await asyncDeviceAction();
        }
        catch (Exception e)
        {
          Exception?.Invoke(asyncDeviceAction, new ThreadExceptionEventArgs(e));
        }
        finally
        {
          DeviceActionTaskTracker.Remove(asyncDeviceAction);
          DeviceActionCompleted?.Invoke(asyncDeviceAction, EventArgs.Empty);
        }
      });
    }

    /// <summary>
    ///   Gets the <see cref="Task" /> for the device action asynchronously being executed at the moment.
    /// </summary>
    /// <param name="deviceAction">
    ///   The device action to get the <see cref="Task" /> for.
    /// </param>
    /// <returns>
    ///   A <see cref="Task" /> object for the running device action or a <see cref="Task.CompletedTask" /> if the
    ///   specified device action is not being executed at the moment.
    /// </returns>
    public static Task GetDeviceActionTask(Action deviceAction) =>
      DeviceActionTaskTracker.TryGetValue(deviceAction, out var task) ? task : Task.CompletedTask;

    /// <summary>
    ///   Gets the <see cref="Task" /> for the device action asynchronously being executed at the moment.
    /// </summary>
    /// <param name="asyncDeviceAction">
    ///   The asynchronous device action to get the <see cref="Task" /> for.
    /// </param>
    /// <returns>
    ///   A <see cref="Task" /> object for the running device action or a <see cref="Task.CompletedTask" /> if the
    ///   specified device action is not being executed at the moment.
    /// </returns>
    public static Task GetDeviceActionTask(Func<Task> asyncDeviceAction) =>
      DeviceActionTaskTracker.TryGetValue(asyncDeviceAction, out var task) ? task : Task.CompletedTask;

    /// <summary>
    ///   Waits for all running device actions to complete.
    /// </summary>
    public static async Task WaitForAllActionsToCompleteAsync() =>
      await Task.WhenAll(DeviceActionTaskTracker.Values.ToArray());
  }
}
