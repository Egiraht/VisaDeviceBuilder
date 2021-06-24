namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for builders creating VISA devices.
  /// </summary>
  public interface IVisaDeviceBuilder
  {
    /// <summary>
    ///   Builds a new VISA device instance.
    /// </summary>
    IVisaDevice BuildVisaDevice();

    /// <summary>
    ///   Builds a new VISA device instance and returns a VISA device controller attached to it.
    /// </summary>
    IVisaDeviceController BuildVisaDeviceController();
  }
}
