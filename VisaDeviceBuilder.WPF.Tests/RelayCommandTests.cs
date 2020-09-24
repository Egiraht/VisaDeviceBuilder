using System;
using System.Threading.Tasks;
using VisaDeviceBuilder.WPF.Components;
using Xunit;

namespace VisaDeviceBuilder.WPF.Tests
{
  public class RelayCommandTests
  {
    private const string TestString = "Test string";

    private const int AsyncActionDelay = 10;

    private object? Value { get; set; }

    private bool CanExecute { get; set; } = false;

    [Fact]
    public void CommandActionTest()
    {
      var command = new RelayCommand(newValue => Value = newValue, _ => CanExecute);
      Assert.Null(Value);
      Assert.False(CanExecute);
      Assert.NotNull(command.Action);
      Assert.NotNull(command.Condition);
      Assert.False(command.CanExecute());

      command.Execute(TestString);
      Assert.Null(Value);

      command.Action!.Invoke(TestString);
      Assert.Equal(TestString, Value);

      Value = null;
      CanExecute = true;
      Assert.Null(Value);
      Assert.True(command.CanExecute());

      command.Execute(TestString);
      Assert.Equal(TestString, Value);
    }

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

      command.Execute(TestString);
      Assert.Null(asyncActionTask);
      Assert.Null(Value);

      command.Action!.Invoke(TestString);
      Assert.Null(Value);
      await asyncActionTask!;
      Assert.Equal(TestString, Value);

      Value = null;
      CanExecute = true;
      Assert.Null(Value);
      Assert.True(command.CanExecute());

      command.Execute(TestString);
      Assert.Null(Value);
      await asyncActionTask!;
      Assert.Equal(TestString, Value);
    }

    [Fact]
    public void CanExecuteExceptionTest()
    {
      var command = new RelayCommand(newValue => Value = newValue, _ => throw new Exception());
      Assert.False(command.CanExecute());

      command.Execute(TestString);
      Assert.Null(Value);

      command.Action!.Invoke(TestString);
      Assert.Equal(TestString, Value);
    }
  }
}
