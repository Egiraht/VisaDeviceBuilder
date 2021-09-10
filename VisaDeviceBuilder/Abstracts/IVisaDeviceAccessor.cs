// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin <stibiu@yandex.ru>

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The common interface for objects that control VISA device instances.
  /// </summary>
  public interface IVisaDeviceAccessor
  {
    /// <summary>
    ///   Gets the VISA device instance attached to this object.
    /// </summary>
    IVisaDevice Device { get; }
  }
}
