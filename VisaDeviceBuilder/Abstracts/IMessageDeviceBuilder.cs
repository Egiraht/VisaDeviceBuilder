namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for builders creating VISA message-based devices.
  /// </summary>
  public interface IMessageDeviceBuilder : IVisaDeviceBuilder
  {
    /// <summary>
    ///   Builds a new message-based VISA device instance.
    /// </summary>
    new IMessageDevice BuildVisaDevice();
  }
}
