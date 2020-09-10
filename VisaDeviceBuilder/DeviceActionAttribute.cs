using System;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The attribute indicating that the decorated method can be treated as a device action and
  ///   should be added to the device's <see cref="IVisaDevice.DeviceActions" /> dictionary.
  ///   The decorated method must have a <see cref="Action" /> delegate signature (no parameters and
  ///   no return value).
  /// </summary>
  [AttributeUsage(AttributeTargets.Method)]
  public class DeviceActionAttribute : Attribute
  {
  }
}
