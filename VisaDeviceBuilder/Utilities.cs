using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The static class with various utility methods.
  /// </summary>
  [ExcludeFromCodeCoverage]
  public static class Utilities
  {
    /// <summary>
    ///   Validates if the provided method type info matches the provided delegate type.
    /// </summary>
    /// <param name="methodInfo">
    ///   The method type info to validate.
    /// </param>
    /// <typeparam name="TDelegate">
    ///   The delegate type the <paramref name="methodInfo" /> must match.
    /// </typeparam>
    /// <returns>
    ///   <c>true</c> if the <paramref name="methodInfo" /> matches the type of <typeparamref name="TDelegate" />,
    ///   otherwise <c>false</c>.
    /// </returns>
    public static bool ValidateDelegate<TDelegate>(MethodInfo methodInfo) where TDelegate : Delegate
    {
      var delegateType = typeof(TDelegate).GetMethod(nameof(Action.Invoke))!;
      return delegateType.ReturnType == methodInfo.ReturnType &&
        delegateType.GetParameters().Select(parameter => parameter.ParameterType)
          .SequenceEqual(methodInfo.GetParameters().Select(parameter => parameter.ParameterType));
    }

    /// <summary>
    ///   Validates if the provided method matches the provided delegate type.
    /// </summary>
    /// <param name="method">
    ///   The method to validate.
    /// </param>
    /// <typeparam name="TDelegate">
    ///   The delegate type the <paramref name="method" /> must match.
    /// </typeparam>
    /// <returns>
    ///   <c>true</c> if the <paramref name="method" /> matches the type of <typeparamref name="TDelegate" />,
    ///   otherwise <c>false</c>.
    /// </returns>
    public static bool ValidateDelegate<TDelegate>(Delegate method) where TDelegate : Delegate =>
      ValidateDelegate<TDelegate>(method.GetType().GetMethod(nameof(Action.Invoke))!);
  }
}
