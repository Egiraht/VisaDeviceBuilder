using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   A builder class that can build VISA devices with custom behavior.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This builder class is intended for building VISA devices that require using custom non-message-based VISA
  ///     session implementations. For the most commonly used case of message-based device communication (e.g. SCPI
  ///     language-based communication), consider using the <see cref="MessageDeviceBuilder" /> class instead of this
  ///     one.
  ///   </para>
  ///   <para>
  ///     After a VISA device is built, the current builder instance cannot be reused. Create a new builder if
  ///     necessary.
  ///   </para>
  /// </remarks>
  public class VisaDeviceBuilder : VisaDeviceBuilder<BuildableVisaDevice, IVisaDevice>
  {
  }
}
