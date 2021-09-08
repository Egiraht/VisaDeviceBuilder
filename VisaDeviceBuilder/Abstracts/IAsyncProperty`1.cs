// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The interface for asynchronous properties with values of type <typeparamref name="TValue" />.
  /// </summary>
  /// <typeparam name="TValue">
  ///   The type of the value this asynchronous property can access.
  /// </typeparam>
  public interface IAsyncProperty<TValue> : IAsyncProperty
  {
    /// <inheritdoc cref="IAsyncProperty.Getter" />
    new TValue Getter { get; }

    /// <inheritdoc cref="IAsyncProperty.Setter" />
    new TValue Setter { set; }
  }
}
