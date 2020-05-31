using System;

namespace VisaDeviceBuilder.Exceptions
{
  /// <summary>
  ///   The class representing a VISA device exception caused by a VISA session access failure.
  /// </summary>
  class VisaSessionException : VisaDeviceException
  {
    /// <summary>
    ///   Creates a new VISA session exception instance.
    /// </summary>
    /// <param name="device">
    ///   The VISA device instance that has thrown this exception.
    /// </param>
    public VisaSessionException(IVisaDevice device) : base(device)
    {
    }

    /// <summary>
    ///   Creates a new VISA session exception instance.
    /// </summary>
    /// <param name="device">
    ///   The VISA device instance that has thrown this exception.
    /// </param>
    /// <param name="message">
    ///   The message describing the exception.
    /// </param>
    public VisaSessionException(IVisaDevice device, string message) : base(device, message)
    {
    }

    /// <summary>
    ///   Creates a new VISA session exception instance.
    /// </summary>
    /// <param name="device">
    ///   The VISA device instance that has thrown this exception.
    /// </param>
    /// <param name="innerException">
    ///   The inner exception instance.
    /// </param>
    /// <param name="message">
    ///   The optional message describing the exception.
    /// </param>
    public VisaSessionException(IVisaDevice device, Exception innerException, string message = "") :
      base(device, innerException, message)
    {
    }

    /// <inheritdoc />
    public override string Message => !string.IsNullOrEmpty(base.Message)
      ? base.Message
      : "An exception " +
      (InnerException != null ? $"of type {InnerException.GetType().Name} " : "") +
      $"was thrown while accessing a session for the VISA device \"{Device.AliasName}\"" +
      (InnerException != null ? $": {InnerException.Message} " : ".");
  }
}
