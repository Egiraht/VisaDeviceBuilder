using System;
using System.Threading.Tasks;
using VisaDeviceBuilder.WPF.Components;
using Xunit;

namespace VisaDeviceBuilder.WPF.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="RelayCommand" /> class.
  /// </summary>
  public class RelayCommandTests
  {
    /// <summary>
    ///   Defines the asynchronous device action delay to simulate long operations.
    /// </summary>
    private const int AsyncActionDelay = 1;

    /// <summary>
    ///   Defines the test string value.
    /// </summary>
    private const string TestString = "Test string";

    /// <summary>
    ///   Gets or sets the test value object.
    /// </summary>
    private object? Value { get; set; }

    /// <summary>
    ///   Gets ort sets the flag defining if the test relay command can be executed.
    /// </summary>
    private bool CanExecute { get; set; }

    /// <summary>
    ///   Testing synchronous command actions execution.
    /// </summary>
    [Fact]
    public void CommandActionTest()
    {
      var command = new RelayCommand(newValue => Value = newValue, _ => CanExecute);
      Assert.Null(Value);
      Assert.False(CanExecute);
      Assert.NotNull(command.Action);
      Assert.NotNull(command.Condition);
      Assert.False(command.CanExecute());

      // The action must not execute (CanExecute() == false).
      command.Execute(TestString);
      Assert.Null(Value);

      // The action executes always (direct call).
      command.Action!.Invoke(TestString);
      Assert.Equal(TestString, Value);

      Value = null;
      CanExecute = true;
      Assert.Null(Value);
      Assert.True(command.CanExecute());

      // The action must execute (CanExecute() == true).
      command.Execute(TestString);
      Assert.Equal(TestString, Value);
    }

    /// <summary>
    ///   Testing asynchronous command actions execution.
    /// </summary>
    [Fact]
    public async Task CommandAsyncActionTest()
    {
      Task? asyncActionTask = null;
      Func<object?, Task> asyncAction = newValue => asyncActionTask = Task.Run(async () =>
      {
        await Task.Delay(AsyncActionDelay);
        Value = newValue;
      });
      var command = new RelayCommand(asyncAction, _ => CanExecute);
      Assert.Null(Value);
      Assert.False(CanExecute);
      Assert.NotNull(command.Action);
      Assert.NotNull(command.Condition);
      Assert.False(command.CanExecute());

      // The action must not execute (CanExecute() == false).
      command.Execute(TestString);
      Assert.Null(asyncActionTask);
      Assert.Null(Value);

      // The action executes always (direct call).
      command.Action!.Invoke(TestString);
      Assert.Null(Value);
      await asyncActionTask!;
      Assert.Equal(TestString, Value);

      Value = null;
      CanExecute = true;
      Assert.Null(Value);
      Assert.True(command.CanExecute());

      // The action must execute (CanExecute() == true).
      command.Execute(TestString);
      Assert.Null(Value);
      await asyncActionTask!;
      Assert.Equal(TestString, Value);
    }

    /// <summary>
    ///   Testing the exceptions handling during the <see cref="RelayCommand.CanExecute" /> method evaluation.
    /// </summary>
    [Fact]
    public void CanExecuteExceptionTest()
    {
      var command = new RelayCommand(newValue => Value = newValue, _ => throw new Exception());
      Assert.False(command.CanExecute());

      // The action must not execute (CanExecute() method handles the exception and returns false).
      command.Execute(TestString);
      Assert.Null(Value);

      // The action executes always (direct call).
      command.Action!.Invoke(TestString);
      Assert.Equal(TestString, Value);
    }
  }
}
