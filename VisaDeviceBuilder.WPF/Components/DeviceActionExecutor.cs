using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace VisaDeviceBuilder.WPF.Components
{
  /// <summary>
  ///   The singleton class that executes device actions and tracks their completion states.
  ///   The main class instance can be accessed via the <see cref="Instance" /> static property.
  ///   This instance can be used as a binding source for the <see cref="ICommandSource.Command" /> dependency property
  ///   in any control implementing the <see cref="ICommandSource" /> interface. In this case the asynchronous
  ///   action to be executed must be provided to the control's <see cref="ICommandSource.CommandParameter" />
  ///   dependency property.
  /// </summary>
  public class DeviceActionExecutor : ICommand
  {
    /// <summary>
    ///   The backing field for the <see cref="Instance" /> property.
    /// </summary>
    private static DeviceActionExecutor? _instance;

    /// <summary>
    ///   Gets the static dictionary that tracks the states of all started device action tasks.
    /// </summary>
    private static Dictionary<Action, Task> DeviceActionTaskTracker { get; } = new Dictionary<Action, Task>();

    /// <summary>
    ///   Gets the singleton instance of the class.
    /// </summary>
    public static DeviceActionExecutor Instance => _instance ??= new DeviceActionExecutor();

    /// <summary>
    ///   Checks if no device actions are running at the moment.
    /// </summary>
    public static bool NoDeviceActionsAreRunning =>
      !DeviceActionTaskTracker.Any() || DeviceActionTaskTracker.All(pair => pair.Value.IsCompleted);

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    ///   The event called when an exception is thrown during the device action processing.
    /// </summary>
    public event ThreadExceptionEventHandler? Exception;

    /// <summary>
    ///   The private singleton class constructor.
    /// </summary>
    private DeviceActionExecutor()
    {
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => parameter is Action deviceAction &&
      (!DeviceActionTaskTracker.ContainsKey(deviceAction) || DeviceActionTaskTracker[deviceAction].IsCompleted);

    /// <inheritdoc />
    public async void Execute(object? parameter)
    {
      if (!(parameter is Action deviceAction))
        return;

      try
      {
        DeviceActionTaskTracker[deviceAction] = Task.Run(() => deviceAction());
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        await DeviceActionTaskTracker[deviceAction];
      }
      catch (Exception e)
      {
        Exception?.Invoke(this, new ThreadExceptionEventArgs(e));
      }
      finally
      {
        DeviceActionTaskTracker.Remove(deviceAction);
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    /// <summary>
    ///   Waits for all running device actions to complete.
    /// </summary>
    public static async Task WaitForAllActionsToCompleteAsync()
    {
      foreach (var (_, task) in DeviceActionTaskTracker)
      {
        try
        {
          await task;
        }
        catch
        {
          // Suppress exceptions.
        }
      }
    }
  }
}
