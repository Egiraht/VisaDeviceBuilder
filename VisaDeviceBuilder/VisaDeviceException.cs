using System;
using System.Diagnostics.CodeAnalysis;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The class representing a VISA device exception.
  /// </summary>
  [ExcludeFromCodeCoverage]
  public class VisaDeviceException : VisaException
  {
    /// <summary>
    ///   Get the optional VISA device instance that has caused the exception.
    /// </summary>
    public IVisaDevice? Device { get; }

    /// <summary>
    ///   Creates a new VISA device exception instance.
    /// </summary>
    /// <param name="device">
    ///   The optional reference to the VISA device instance that has thrown this exception.
    /// </param>
    public VisaDeviceException(IVisaDevice? device) => Device = device;

    /// <summary>
    ///   Creates a new VISA device exception instance.
    /// </summary>
    /// <param name="device">
    ///   The optional reference to the VISA device instance that has thrown this exception.
    /// </param>
    /// <param name="innerException">
    ///   The inner exception instance actually describing the problem.
    /// </param>
    public VisaDeviceException(IVisaDevice? device, Exception? innerException) :
      base(string.Empty, innerException) => Device = device;

    /// <inheritdoc />
    public override string Message => !string.IsNullOrEmpty(base.Message)
      ? base.Message
      : "A VISA device-related exception" +
      (InnerException != null ? $" of type {InnerException.GetType().Name}" : "") +
      " has been thrown" +
      (Device != null ? $" by the VISA device \"{Device.AliasName}\"" : "") +
      (InnerException != null ? $": {InnerException.Message} " : ".");
  }
}
