// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin <stibiu@yandex.ru>

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   Creates a new VISA resource name record. This record can be implicitly cast into a string containing the stored
  ///   canonical VISA resource name.
  /// </summary>
  /// <param name="CanonicalName">
  ///   The canonical VISA resource name stored in this record.
  /// </param>
  /// <param name="AliasName">
  ///   The optional VISA resource alias name stored in this record.
  /// </param>
  public record VisaResourceName(string CanonicalName, string? AliasName = null)
  {
    /// <summary>
    ///   Implicitly casts an instance of <see cref="VisaResourceName" /> into a string containing the corresponding
    ///   canonical VISA resource name.
    /// </summary>
    /// <param name="visaResourceName">
    ///   The VISA resource name record instance to cast.
    /// </param>
    /// <returns>
    ///   A string containing the canonical VISA resource name stored in the instance.
    /// </returns>
    public static implicit operator string(VisaResourceName visaResourceName) => visaResourceName.CanonicalName;

    /// <summary>
    ///   Converts this record into a user-readable form.
    /// </summary>
    /// <returns>
    ///   A string like <c>"AliasName (CanonicalName)"</c> if the stored alias name is available, otherwise returns only
    ///   the stored canonical name.
    /// </returns>
    public override string ToString() =>
      string.IsNullOrWhiteSpace(AliasName) ? CanonicalName : $"{AliasName} ({CanonicalName})";
  }
}
