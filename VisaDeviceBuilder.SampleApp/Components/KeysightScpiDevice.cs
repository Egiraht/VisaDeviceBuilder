using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// ReSharper disable StringLiteralTypo
namespace VisaDeviceBuilder.SampleApp.Components
{
  /// <summary>
  ///   The base class for VISA devices manufactured by <i>Keysight Technologies Inc.</i> and supporting communication
  ///   based on SCPI commands.
  /// </summary>
  public class KeysightScpiDevice : MessageDevice
  {
    /// <summary>
    ///   Gets the oldest error message from the device error queue.
    ///   When there are no errors in the queue, the message starting with the code <c>0</c> or <c>+0</c> is read.
    /// </summary>
    /// <returns>
    ///   The oldest error message string consisting from the status code and error description.
    /// </returns>
    protected virtual string GetError() => base.SendMessage("SYST:ERR?");

    /// <summary>
    ///   Synchronously sends the message to the connected SCPI device.
    /// </summary>
    /// <param name="message">
    ///   The message containing the SCPI command.
    /// </param>
    /// <returns>
    ///   The SCPI command response.
    /// </returns>
    /// <exception cref="VisaDeviceException">
    ///   The message processing failed or the SCPI command response contains one or more errors.
    /// </exception>
    public override string SendMessage(string message)
    {
      lock (SessionLock)
      {
        try
        {
          message = message.Trim();

          // Sending the message and trying to get confirmation.
          if (message.Contains('?'))
            return base.SendMessage($"*CLS;{message}").TrimEnd('\x0D', '\x0A');
          var response = base.SendMessage($"*CLS;{message};*OPC?").TrimEnd('\x0D', '\x0A');
          return Regex.Replace(response, @";?1$", string.Empty);
        }
        catch (Exception e)
        {
          var errorStack = new List<string>();

          // Trying to get the list of device errors.
          try
          {
            var error = GetError();
            while (!error.StartsWith("0") && !error.StartsWith("+0"))
            {
              errorStack.Add(error);
              error = GetError();
            }
          }
          catch
          {
            // Ignore exceptions.
          }

          if (errorStack.Any())
            throw new VisaDeviceException(this, new InvalidDataException(string.Join("; ", errorStack)));

          throw new VisaDeviceException(this, e);
        }
      }
    }

    /// <summary>
    ///   Asynchronously sends the message to the connected SCPI device.
    /// </summary>
    /// <param name="message">
    ///   The message containing the SCPI command.
    /// </param>
    /// <returns>
    ///   The SCPI command response.
    /// </returns>
    /// <exception cref="VisaDeviceException">
    ///   The message processing failed or the SCPI command response contains one or more errors.
    /// </exception>
    public override Task<string> SendMessageAsync(string message) => Task.Run(() => SendMessage(message));

    /// <summary>
    ///   Reads the device identifier string using the <c>*IDN?</c> SCPI command.
    /// </summary>
    /// <returns>
    ///   The device identification string containing the manufacturer name, the model number, the software version,
    ///   and the device serial number.
    /// </returns>
    public override string GetIdentifier() => SendMessage("*IDN?");

    /// <summary>
    ///   Asynchronously reads the device identifier string using the <c>*IDN?</c> SCPI command.
    /// </summary>
    /// <returns>
    ///   The device identification string containing the manufacturer name, the model number, the software version,
    ///   and the device serial number.
    /// </returns>
    public override Task<string> GetIdentifierAsync() => SendMessageAsync("*IDN?");

    /// <summary>
    ///   Resets the device to some predefined state using the <c>*RST</c> SCPI command.
    /// </summary>
    public override void Reset() => SendMessage("*RST");

    /// <summary>
    ///   Asynchronously resets the device to some predefined state using the <c>*RST</c> SCPI command.
    /// </summary>
    public override Task ResetAsync() => SendMessageAsync("*RST");

    /// <summary>
    ///   Clears the device event and status registers using the <c>*CLS</c> SCPI command.
    /// </summary>
    protected virtual void ClearStatus() => SendMessage("*CLS");

    /// <summary>
    ///   Asynchronously clears the device event and status registers using the <c>*CLS</c> SCPI command.
    /// </summary>
    protected virtual Task ClearStatusAsync() => SendMessageAsync("*CLS");

    /// <inheritdoc />
    protected override void Initialize()
    {
      base.Initialize();
      ClearStatus();
    }
  }
}