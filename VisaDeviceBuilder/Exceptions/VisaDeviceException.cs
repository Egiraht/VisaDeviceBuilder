using System;
using Ivi.Visa;

namespace VisaDeviceBuilder.Exceptions
{
  /// <summary>
  ///   The class representing a VISA device exception.
  /// </summary>
  public class VisaDeviceException : VisaException
  {
    public IVisaDevice Device { get; }

    /// <summary>
    ///   Creates a new VISA device exception instance.
    /// </summary>
    /// <param name="device">
    ///   The VISA device instance that has thrown this exception.
    /// </param>
    public VisaDeviceException(IVisaDevice device)
    {
      Device = device;
    }

    /// <summary>
    ///   Creates a new VISA device exception instance.
    /// </summary>
    /// <param name="device">
    ///   The VISA device instance that has thrown this exception.
    /// </param>
    /// <param name="message">
    ///   The message describing the exception.
    /// </param>
    public VisaDeviceException(IVisaDevice device, string message) : base(message)
    {
      Device = device;
    }

    /// <summary>
    ///   Creates a new VISA device exception instance.
    /// </summary>
    /// <param name="device">
    ///   The VISA device instance that has thrown this exception.
    /// </param>
    /// <param name="message">
    ///   The message describing the exception.
    /// </param>
    /// <param name="innerException">
    ///   The inner exception instance.
    /// </param>
    public VisaDeviceException(IVisaDevice device, string message, Exception innerException) :
      base(message, innerException)
    {
      Device = device;
    }

    /// <inheritdoc />
    public override string Message => !string.IsNullOrEmpty(base.Message)
      ? base.Message
      : "An exception " +
      (InnerException != null ? $"of type {InnerException.GetType().Name} " : "") +
      "was thrown by the VISA device " +
      $"\"{(!string.IsNullOrEmpty(Device.AliasName) ? Device.AliasName : Device.ResourceName)}\".";
  }
}
