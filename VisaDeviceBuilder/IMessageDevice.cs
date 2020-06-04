using System.Threading.Tasks;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The interface describing a message-based VISA device.
  /// </summary>
  public interface IMessageDevice : IVisaDevice
  {
    Task<string?> SendMessageAsync(string request);
  }
}
