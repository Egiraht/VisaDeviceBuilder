using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace VisaDeviceBuilder.WPF.Components
{
  /// <summary>
  ///   The command class that invokes the asynchronous action provided as a parameter and tracks its completion state.
  ///   This command is a singleton and must be accessed via the <see cref="Instance" /> property.
  /// </summary>
  public class InvokeAsyncActionCommand : ICommand
  {
    /// <summary>
    ///   The backing field for the <see cref="Instance" /> property.
    /// </summary>
    private static InvokeAsyncActionCommand? _instance;

    /// <summary>
    ///   Gets the static dictionary that tracks the states of all started asynchronous action tasks.
    /// </summary>
    private static Dictionary<AsyncAction, Task> AsyncActionTaskTracker { get; } = new Dictionary<AsyncAction, Task>();

    /// <summary>
    ///   Gets the singleton instance of the class.
    /// </summary>
    public static InvokeAsyncActionCommand Instance => _instance ??= new InvokeAsyncActionCommand();

    /// <summary>
    ///   The private singleton class constructor.
    /// </summary>
    private InvokeAsyncActionCommand()
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

      AsyncActionTaskTracker[asyncAction] = asyncAction.Invoke();
      CanExecuteChanged?.Invoke(this, EventArgs.Empty);
      await AsyncActionTaskTracker[asyncAction];
      CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;
  }
}
