// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

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
  public static class DelegateExtensions
  {
    /// <summary>
    ///   Validates if the current <see cref="MethodInfo" /> object matches the delegate type
    ///   <paramref name="delegateType" />.
    /// </summary>
    /// <param name="methodInfo">
    ///   The method's <see cref="MethodInfo" /> object to validate to validate the delegate type for.
    /// </param>
    /// <param name="delegateType">
    ///   The delegate type that the <paramref name="methodInfo" /> must match.
    /// </param>
    /// <returns>
    ///   <c>true</c> if the <paramref name="methodInfo" /> matches the type of <paramref name="delegateType" />,
    ///   otherwise <c>false</c>.
    /// </returns>
    public static bool ValidateDelegateType(this MethodInfo methodInfo, Type delegateType)
    {
      if (!delegateType.IsAssignableTo(typeof(Delegate)))
        return false;

      var delegateMethodType = delegateType.GetMethod(nameof(Action.Invoke))!;
      return delegateMethodType.ReturnType == methodInfo.ReturnType &&
        delegateMethodType.GetParameters().Select(parameter => parameter.ParameterType)
          .SequenceEqual(methodInfo.GetParameters().Select(parameter => parameter.ParameterType));
    }

    /// <summary>
    ///   Validates if the current <see cref="MethodInfo" /> object matches the delegate type
    ///   <typeparamref name="TDelegate" />.
    /// </summary>
    /// <typeparam name="TDelegate">
    ///   The delegate type that the <paramref name="methodInfo" /> must match.
    /// </typeparam>
    /// <param name="methodInfo">
    ///   The method's <see cref="MethodInfo" /> object to validate to validate the delegate type for.
    /// </param>
    /// <returns>
    ///   <c>true</c> if the <paramref name="methodInfo" /> matches the type of <typeparamref name="TDelegate" />,
    ///   otherwise <c>false</c>.
    /// </returns>
    public static bool ValidateDelegateType<TDelegate>(this MethodInfo methodInfo) where TDelegate : Delegate =>
      methodInfo.ValidateDelegateType(typeof(TDelegate));

    /// <summary>
    ///   Validates if the current delegate matches the delegate type <paramref name="delegateType" />.
    /// </summary>
    /// <param name="delegate">
    ///   The delegate to validate.
    /// </param>
    /// <param name="delegateType">
    ///   The delegate type that the <paramref name="delegate" /> must match.
    /// </param>
    /// <returns>
    ///   <c>true</c> if the <paramref name="delegate" /> matches the type of <paramref name="delegateType" />,
    ///   otherwise <c>false</c>.
    /// </returns>
    public static bool ValidateDelegateType(this Delegate @delegate, Type delegateType) =>
      @delegate.GetType().GetMethod(nameof(Action.Invoke))!.ValidateDelegateType(delegateType);

    /// <summary>
    ///   Validates if the current delegate matches the delegate type <typeparamref name="TDelegate" />.
    /// </summary>
    /// <typeparam name="TDelegate">
    ///   The delegate type that the <paramref name="delegate" /> must match.
    /// </typeparam>
    /// <param name="delegate">
    ///   The method to validate.
    /// </param>
    /// <returns>
    ///   <c>true</c> if the <paramref name="delegate" /> matches the type of <typeparamref name="TDelegate" />,
    ///   otherwise <c>false</c>.
    /// </returns>
    public static bool ValidateDelegateType<TDelegate>(this Delegate @delegate) where TDelegate : Delegate =>
      @delegate.ValidateDelegateType(typeof(TDelegate));
  }
}
