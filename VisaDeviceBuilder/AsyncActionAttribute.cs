using System;
using System.Threading.Tasks;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The attribute indicating that the decorated method can be treated as an asynchronous action and
  ///   should be added to the device's <see cref="IVisaDevice.AsyncActions" /> dictionary.
  ///   The decorated method must have a <see cref="AsyncAction" /> delegate signature (no parameters and
  ///   <see cref="Task" /> return type).
  /// </summary>
  [AttributeUsage(AttributeTargets.Method)]
  public class AsyncActionAttribute : Attribute
  {
  }
}
