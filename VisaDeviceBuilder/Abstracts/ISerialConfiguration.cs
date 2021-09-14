// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin <stibiu@yandex.ru>

using System;
using Ivi.Visa;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The interface for classes that store and operate serial interface configuration.
  /// </summary>
  public interface ISerialConfiguration : ICloneable
  {
    /// <summary>
    ///   Gets or sets the serial interface baud rate in bits per second.
    /// </summary>
    int BaudRate { get; set; }

    /// <summary>
    ///   Gets or sets the number of data bits contained in each frame (5, 6, 7, or 8).
    /// </summary>
    short DataBits { get; set; }

    /// <summary>
    ///   Gets or sets the parity bit mode to be used in data frames.
    /// </summary>
    SerialParity Parity { get; set; }

    /// <summary>
    ///   Gets or sets the number of stop bits terminating every data frame.
    /// </summary>
    SerialStopBitsMode StopBits { get; set; }

    /// <summary>
    ///   Gets or sets the flow control mode to be used for serial data control.
    /// </summary>
    SerialFlowControlModes FlowControl { get; set; }

    /// <summary>
    ///   Gets or sets the initial state of the Data Terminal Ready (DTR) output signal.
    /// </summary>
    LineState DataTerminalReadyState { get; set; }

    /// <summary>
    ///   Gets or sets the initial state of the Request To Send (RTS) output signal.
    /// </summary>
    LineState RequestToSendState { get; set; }

    /// <summary>
    ///   Gets or sets the method used to terminate serial data read operations.
    /// </summary>
    SerialTerminationMethod ReadTermination { get; set; }

    /// <summary>
    ///   Gets or sets the method used to terminate serial data write operations.
    /// </summary>
    SerialTerminationMethod WriteTermination { get; set; }

    /// <summary>
    ///   Gets or sets the character used to replace incoming data frames received with errors.
    /// </summary>
    byte ReplacementCharacter { get; set; }

    /// <summary>
    ///   Gets or sets the <i>XOFF</i> character used for <i>XON/XOFF</i> flow control in both directions.
    /// </summary>
    byte XOffCharacter { get; set; }

    /// <summary>
    ///   Gets or sets the <i>XON</i> character used for <i>XON/XOFF</i> flow control in both directions.
    /// </summary>
    byte XOnCharacter { get; set; }
  }
}
