using System;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The attribute indicating that the decorated method with <see cref="AsyncAction" /> signature (asynchronous
  ///   action) is to be added to the VISA device's <see cref="IVisaDevice.AsyncActions" /> dictionary.
  /// </summary>
  [AttributeUsage(AttributeTargets.Method)]
  public class AsyncActionAttribute : Attribute
  {
    /// <summary>
    ///   Creates a new instance of the attribute.
    /// </summary>
    public AsyncActionAttribute()
    {
    }
  }
}
