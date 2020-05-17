using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The common interface for remote properties.
  /// </summary>
  public interface IRemoteProperty : INotifyPropertyChanged
  {
    /// <summary>
    ///   Gets the name of the remote property.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///   Checks if the remote property is read-only.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    ///   Sets the new string value of the remote property.
    /// </summary>
    string Setter { set; }

    /// <summary>
    ///   Gets the current string value of the remote property.
    /// </summary>
    string Getter { get; }

    /// <summary>
    ///   Checks if the property has been modified since the last remote property synchronization.
    /// </summary>
    bool IsModified { get; }

    /// <summary>
    ///   Checks if the remote property is being synchronized at the moment.
    /// </summary>
    bool IsSynchronizing { get; }

    /// <summary>
    ///   Gets the request message string used for synchronization of the <see cref="Getter" /> value.
    /// </summary>
    public string GetterRequest { get; }

    /// <summary>
    ///   Gets the request formatter delegate used for formatting the <see cref="Setter" /> value into the request
    ///   message.
    /// </summary>
    Converter<string, string> SetterRequestFormatter { get; }

    /// <summary>
    ///   Gets the response parser delegate used for parsing the response message into the new <see cref="Getter" />
    ///   value.
    /// </summary>
    Converter<string, string> GetterResponseParser { get; }

    /// <summary>
    ///   Asynchronously synchronizes the remote property value between the current software instance and the remote
    ///   device.
    /// </summary>
    /// <param name="requestProvider">
    ///   The request provider used for synchronization.
    /// </param>
    Task SynchronizeAsync(IMessageRequestProvider requestProvider);
  }
}
