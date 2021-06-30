using System;
using System.Diagnostics.CodeAnalysis;

namespace VisaDeviceBuilder.Tests.Components
{
  /// <summary>
  ///   A test exception class.
  /// </summary>
  [ExcludeFromCodeCoverage]
  public class TestException : Exception
  {
    /// <summary>
    ///   Defines the test exception text.
    /// </summary>
    public const string TestExceptionText = "Test exception";

    /// <inheritdoc cref="Exception()" />
    public TestException() : base(TestExceptionText)
    {
    }

    /// <inheritdoc cref="Exception(string)" />
    public TestException(string? message) : base(message)
    {
    }

    /// <inheritdoc cref="Exception(string, Exception)" />
    public TestException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
  }
}
