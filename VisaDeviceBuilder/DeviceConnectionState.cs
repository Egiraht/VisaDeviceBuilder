namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The enumeration describing possible device connection states.
  /// </summary>
  public enum DeviceConnectionState
  {
    /// <summary>
    ///   The device is disconnected.
    /// </summary>
    Disconnected,

    /// <summary>
    ///   The device is being initialized.
    /// </summary>
    Initializing,

    /// <summary>
    ///   The device is connected.
    /// </summary>
    Connected,

    /// <summary>
    ///   The device is being de-initialized.
    /// </summary>
    DeInitializing,

    /// <summary>
    ///   The device has been disconnected because of an error.
    /// </summary>
    DisconnectedWithError
  }
}
