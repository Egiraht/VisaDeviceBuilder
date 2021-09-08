// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

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
