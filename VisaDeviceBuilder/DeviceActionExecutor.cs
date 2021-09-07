using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;

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
    private static Dictionary<IDeviceAction, Task> DeviceActionTaskTracker { get; } = new();

    /// <summary>
    ///   Checks if no device actions are running at the moment.
    /// </summary>
    public static bool NoDeviceActionsAreRunning =>
      !DeviceActionTaskTracker.Any() || DeviceActionTaskTracker.All(pair => pair.Value.IsCompleted);

    /// <summary>
    ///   The event called when an exception is thrown during device action processing.
    ///   The event sender object contains the device action object that has raised the event.
    ///   The event arguments object contains the exception instance that has raised the event.
    /// </summary>
    public static event ThreadExceptionEventHandler? Exception;

    /// <summary>
    ///   The event called when any of running device actions completes.
    ///   The event sender object contains the device action object that has raised the event.
    /// </summary>
    public static event EventHandler? DeviceActionCompleted;

    /// <summary>
    ///   Gets an awaitable <see cref="Task" /> for the provided device action object being executed.
    /// </summary>
    /// <param name="deviceAction">
    ///   The device action object to get a <see cref="Task" /> for.
    /// </param>
    /// <returns>
    ///   A <see cref="Task" /> object for the provided <paramref name="deviceAction" /> if it is being executed at the
    ///   moment, or a <see cref="Task.CompletedTask" /> otherwise.
    /// </returns>
    public static Task GetDeviceActionTask(IDeviceAction deviceAction) =>
      DeviceActionTaskTracker.TryGetValue(deviceAction, out var task) ? task : Task.CompletedTask;

    /// <summary>
    ///   Checks if the provided device action can be executed at the moment.
    /// </summary>
    /// <param name="deviceAction">
    ///   An <see cref="IDeviceAction" /> object to check.
    /// </param>
    /// <returns>
    ///   <c>true</c> if the provided device action can be executed at the moment, otherwise <c>false</c>.
    /// </returns>
    public static bool CanExecute(IDeviceAction deviceAction) => GetDeviceActionTask(deviceAction).IsCompleted;

    /// <summary>
    ///   Starts asynchronous execution of the provided asynchronous device action delegate if it is not already being
    ///   executed.
    /// </summary>
    /// <param name="deviceAction">
    ///   The <see cref="IDeviceAction" /> object to execute.
    /// </param>
    public static void BeginExecute(IDeviceAction deviceAction)
    {
      if (!CanExecute(deviceAction))
        return;

      DeviceActionTaskTracker[deviceAction] = Task.Run(() =>
      {
        try
        {
          deviceAction.DeviceActionDelegate.Invoke(deviceAction.TargetDevice);
        }
        catch (Exception e)
        {
          OnException(deviceAction, e);
        }
        finally
        {
          DeviceActionTaskTracker.Remove(deviceAction);
          OnDeviceActionCompleted(deviceAction);
        }
      });
    }

    /// <summary>
    ///   Waits for all running device actions to complete.
    /// </summary>
    public static async Task WaitForAllActionsToCompleteAsync() => await Task.WhenAll(DeviceActionTaskTracker.Values);

    /// <summary>
    ///   Invokes the <see cref="Exception" /> event.
    /// </summary>
    /// <param name="sender">
    ///   The <see cref="IDeviceAction" /> object that has thrown an exception.
    /// </param>
    /// <param name="exception">
    ///   The exception thrown during device action execution.
    /// </param>
    private static void OnException(IDeviceAction sender, Exception exception) =>
      Exception?.Invoke(sender, new ThreadExceptionEventArgs(exception));

    /// <summary>
    ///   Invokes the <see cref="DeviceActionCompleted" /> event.
    /// </summary>
    /// <param name="sender">
    ///   The <see cref="IDeviceAction" /> object that has completed its execution.
    /// </param>
    private static void OnDeviceActionCompleted(IDeviceAction sender) =>
      DeviceActionCompleted?.Invoke(sender, EventArgs.Empty);
  }
}
