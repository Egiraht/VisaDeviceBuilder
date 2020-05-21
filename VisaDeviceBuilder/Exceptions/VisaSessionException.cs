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
    /// <param name="message">
    ///   The message describing the exception.
    /// </param>
    /// <param name="innerException">
    ///   The inner exception instance.
    /// </param>
    public VisaSessionException(IVisaDevice device, string message, Exception innerException) :
      base(device, message, innerException)
    {
    }

    /// <inheritdoc />
    public override string Message => !string.IsNullOrEmpty(base.Message)
      ? base.Message
      : "Failed to access an opened VISA session for the VISA device " +
      $"\"{(!string.IsNullOrEmpty(Device.AliasName) ? Device.AliasName : Device.ResourceName)}\".";
  }
}
