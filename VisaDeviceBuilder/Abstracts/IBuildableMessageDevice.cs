// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for message-based VISA devices that can be built using builders.
  /// </summary>
  public interface IBuildableMessageDevice : IMessageDevice, IBuildableVisaDevice
  {
    /// <summary>
    ///   Gets or sets the custom delegate for request and response messages processing.
    /// </summary>
    Func<IMessageDevice?, string, string>? CustomMessageProcessor { get; set; }
  }
}
