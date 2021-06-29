namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for builders creating VISA devices.
  /// </summary>
  /// <typeparam name="TVisaDevice">
  ///   The type of the VISA device this builder is designed to build.
  /// </typeparam>
  public interface IVisaDeviceBuilder<out TVisaDevice> where TVisaDevice : IVisaDevice
  {
    /// <summary>
    ///   Builds a new VISA device instance.
    /// </summary>
    TVisaDevice BuildDevice();

    /// <summary>
    ///   Builds a new VISA device instance and returns a VISA device controller for it.
    /// </summary>
    IVisaDeviceController BuildDeviceController();
  }
}
