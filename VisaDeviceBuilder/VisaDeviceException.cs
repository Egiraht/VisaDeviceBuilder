using System;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The class representing a VISA device exception.
  /// </summary>
  public class VisaDeviceException : VisaException
  {
    public IVisaDevice? Device { get; }

    /// <summary>
    ///   Creates a new VISA device exception instance.
    /// </summary>
    /// <param name="device">
    ///   The VISA device instance that has thrown this exception.
    /// </param>
    public VisaDeviceException(IVisaDevice? device) => Device = device;

    /// <summary>
    ///   Creates a new VISA device exception instance.
    /// </summary>
    /// <param name="device">
    ///   The VISA device instance that has thrown this exception.
    /// </param>
    /// <param name="message">
    ///   The message describing the exception.
    /// </param>
    public VisaDeviceException(IVisaDevice? device, string message) : base(message) => Device = device;

    /// <summary>
    ///   Creates a new VISA device exception instance.
    /// </summary>
    /// <param name="device">
    ///   The VISA device instance that has thrown this exception.
    /// </param>
    /// <param name="innerException">
    ///   The inner exception instance.
    /// </param>
    public VisaDeviceException(IVisaDevice? device, Exception? innerException) :
      base(string.Empty, innerException) => Device = device;

    /// <summary>
    ///   Creates a new VISA device exception instance.
    /// </summary>
    /// <param name="device">
    ///   The VISA device instance that has thrown this exception.
    /// </param>
    /// <param name="message">
    ///   The optional message describing the exception.
    /// </param>
    /// <param name="innerException">
    ///   The inner exception instance.
    /// </param>
    public VisaDeviceException(IVisaDevice? device, string message, Exception? innerException) :
      base(message, innerException) => Device = device;

    /// <inheritdoc />
    public override string Message => !string.IsNullOrEmpty(base.Message)
      ? base.Message
      : "An exception " +
      (InnerException != null ? $"of type {InnerException.GetType().Name} " : "") +
      "was thrown" +
      (Device != null ? $" by the VISA device \"{Device.AliasName}\"" : "") +
      (InnerException != null ? $": {InnerException.Message} " : ".");
  }
}
