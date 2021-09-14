// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin <stibiu@yandex.ru>

using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The class that stores serial interface configuration.
  /// </summary>
  public class SerialConfiguration : ISerialConfiguration
  {
    /// <inheritdoc />
    public int BaudRate { get; set; } = 9600;

    /// <inheritdoc />
    public short DataBits { get; set; } = 8;

    /// <inheritdoc />
    public SerialParity Parity { get; set; } = SerialParity.None;

    /// <inheritdoc />
    public SerialStopBitsMode StopBits { get; set; } = SerialStopBitsMode.One;

    /// <inheritdoc />
    public SerialFlowControlModes FlowControl { get; set; } = SerialFlowControlModes.None;

    /// <inheritdoc />
    public LineState DataTerminalReadyState { get; set; } = LineState.Asserted;

    /// <inheritdoc />
    public LineState RequestToSendState { get; set; } = LineState.Asserted;

    /// <inheritdoc />
    public SerialTerminationMethod ReadTermination { get; set; } = SerialTerminationMethod.TerminationCharacter;

    /// <inheritdoc />
    public SerialTerminationMethod WriteTermination { get; set; } = SerialTerminationMethod.TerminationCharacter;

    /// <inheritdoc />
    public byte ReplacementCharacter { get; set; } = 0x3F; // '?'

    /// <inheritdoc />
    public byte XOffCharacter { get; set; }

    /// <inheritdoc />
    public byte XOnCharacter { get; set; }

    /// <inheritdoc />
    public object Clone() => MemberwiseClone();
  }
}
