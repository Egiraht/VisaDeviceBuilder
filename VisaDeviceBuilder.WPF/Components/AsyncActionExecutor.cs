using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace VisaDeviceBuilder.WPF.Components
{
  /// <summary>
  ///   The singleton class that executes asynchronous actions and tracks their completion states.
  ///   The main class instance can be accessed via the <see cref="Instance" /> static property.
  ///   This instance can be used as a binding source for the <see cref="ICommandSource.Command" /> dependency property
  ///   in any control implementing the <see cref="ICommandSource" /> interface. In this case the asynchronous
  ///   action to be executed must be provided to the control's <see cref="ICommandSource.CommandParameter" />
  ///   dependency property.
  /// </summary>
  public class AsyncActionExecutor : ICommand
  {
    /// <summary>
    ///   The backing field for the <see cref="Instance" /> property.
    /// </summary>
    private static AsyncActionExecutor? _instance;

    /// <summary>
    ///   Gets the static dictionary that tracks the states of all started asynchronous action tasks.
    /// </summary>
    private static Dictionary<AsyncAction, Task> AsyncActionTaskTracker { get; } = new Dictionary<AsyncAction, Task>();

    /// <summary>
    ///   Gets the singleton instance of the class.
    /// </summary>
    public static AsyncActionExecutor Instance => _instance ??= new AsyncActionExecutor();

    /// <summary>
    ///   Checks if no asynchronous actions are running at the moment.
    /// </summary>
    public bool NoAsyncActionsAreRunning =>
      !AsyncActionTaskTracker.Any() || AsyncActionTaskTracker.All(pair => pair.Value.IsCompleted);

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    ///   The event called when an exception is thrown during the asynchronous action processing.
    /// </summary>
    public event ThreadExceptionEventHandler? Exception;

    /// <summary>
    ///   The private singleton class constructor.
    /// </summary>
    private AsyncActionExecutor()
    {
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => parameter is AsyncAction asyncAction &&
      (!AsyncActionTaskTracker.ContainsKey(asyncAction) || AsyncActionTaskTracker[asyncAction].IsCompleted);

    /// <inheritdoc />
    public async void Execute(object? parameter)
    {
      if (!(parameter is AsyncAction asyncAction))
        return;

      try
      {
        AsyncActionTaskTracker[asyncAction] = asyncAction.Invoke();
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        await AsyncActionTaskTracker[asyncAction];
      }
      catch (Exception e)
      {
        Exception?.Invoke(this, new ThreadExceptionEventArgs(e));
      }
      finally
      {
        AsyncActionTaskTracker.Remove(asyncAction);
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    /// <summary>
    ///   Waits for all running asynchronous actions to complete.
    /// </summary>
    public async Task WaitForAllActionsToCompleteAsync()
    {
      foreach (var (_, task) in AsyncActionTaskTracker)
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
