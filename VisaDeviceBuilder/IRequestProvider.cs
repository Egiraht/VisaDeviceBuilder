using System.Threading.Tasks;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The interface for request providers.
  /// </summary>
  /// <typeparam name="TCarrier">
  ///   The type of the data carrier.
  /// </typeparam>
  public interface IRequestProvider<TCarrier>
  {
    /// <summary>
    ///   Asynchronously sends a request and returns a response.
    /// </summary>
    /// <param name="request">
    ///   The request to send.
    /// </param>
    /// <returns>
    ///   The received response result.
    /// </returns>
    Task<TCarrier> SendRequestAsync(TCarrier request);
  }
}
