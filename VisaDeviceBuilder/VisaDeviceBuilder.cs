using System;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   A builder class that can build VISA devices with custom behavior.
  /// </summary>
  /// <remarks>
  ///   This builder class is intended for building VISA devices that require using custom non-message-based VISA
  ///   session implementations. For the most commonly used case of message-based device communication (e.g. SCPI
  ///   language-based communication), consider using the <see cref="MessageDeviceBuilder" /> class instead of this
  ///   one.
  /// </remarks>
  public class VisaDeviceBuilder : VisaDeviceBuilder<BuildableVisaDevice, IVisaDevice>
  {
    /// <summary>
    ///   Initializes a new VISA device builder instance.
    /// </summary>
    public VisaDeviceBuilder()
    {
    }

    /// <summary>
    ///   Initializes a new VISA device builder instance with building configuration copied from a compatible buildable
    ///   VISA device instance.
    /// </summary>
    /// <param name="device">
    ///   A VISA device instance to copy configuration from. This instance must have been previously built by a
    ///   compatible VISA device builder class and must be an instance of <see cref="BuildableVisaDevice" /> class.
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///   Cannot copy building configuration from the provided VISA device instance because it is not assignable to the
    ///   <see cref="BuildableVisaDevice" /> type.
    /// </exception>
    public VisaDeviceBuilder(IVisaDevice device) : base(device)
    {
    }
  }
}
