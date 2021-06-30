using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo
namespace VisaDeviceBuilder.SampleApp.Components
{
  /// <summary>
  ///   The VISA device class representing a Keysight E364xA series power supply.
  /// </summary>
  public class KeysightE364xA : KeysightScpiDevice
  {
    /// <summary>
    ///   Defines the array of string values that can be interpreted as <c>true</c> boolean values.
    /// </summary>
    private static readonly string[] TrueBooleanStrings = {"true", "on", "1", "+1"};

    /// <summary>
    ///   Gets rhe read/write asynchronous property controlling the power supply output state.
    ///   <c>true</c> - output enabled, <c>false</c> - output disabled.
    /// </summary>
    public IAsyncProperty<bool> IsOutputEnabled => _isOutputEnabled ??= new AsyncProperty<bool>(
      () => TrueBooleanStrings.Contains(SendMessage("OUTP?"), StringComparer.InvariantCultureIgnoreCase),
      value => SendMessage($"OUTP {(value ? "ON" : "OFF")}"));
    private IAsyncProperty<bool>? _isOutputEnabled;

    /// <summary>
    ///   Gets the read/write asynchronous property controlling the target output voltage in volts.
    /// </summary>
    public IAsyncProperty<double> TargetVoltage => _targetVoltage ??= new AsyncProperty<double>(
      () => double.TryParse(SendMessage("VOLT?"), NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var value)
        ? value
        : default,
      value => SendMessage($"VOLT {value}"));
    private IAsyncProperty<double>? _targetVoltage;

    /// <summary>
    ///   Gets the read/write asynchronous property controlling the output current limit in amps.
    /// </summary>
    public IAsyncProperty<double> CurrentLimit => _currentLimit ??= new AsyncProperty<double>(
      () => double.TryParse(SendMessage("CURR?"), NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var value)
        ? value
        : default,
      value => SendMessage($"CURR {value}"));
    private IAsyncProperty<double>? _currentLimit;

    /// <summary>
    ///   Gets the read-only asynchronous property accessing the measured output voltage.
    /// </summary>
    public IAsyncProperty<double> MeasuredVoltage => _measuredVoltage ??= new AsyncProperty<double>(
      () => double.TryParse(SendMessage("MEAS:VOLT?"), NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var value)
        ? value
        : default);
    private IAsyncProperty<double>? _measuredVoltage;

    /// <summary>
    ///   Gets the read-only asynchronous property accessing the measured output current.
    /// </summary>
    public IAsyncProperty<double> MeasuredCurrent => _measuredCurrent ??= new AsyncProperty<double>(
      () => double.TryParse(SendMessage("MEAS:CURR?"), NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var value)
        ? value
        : default);
    private IAsyncProperty<double>? _measuredCurrent;

    /// <summary>
    ///   Gets the read/write asynchronous property controlling the output over-voltage protection (OVP) state.
    ///   <c>true</c> - OVP enabled, <c>false</c> - OVP disabled.
    /// </summary>
    public IAsyncProperty<bool> IsOverVoltageProtectionEnabled => _isOverVoltageProtectionEnabled ??=
      new AsyncProperty<bool>(
        () => TrueBooleanStrings.Contains(SendMessage("VOLT:PROT:STAT?"), StringComparer.InvariantCultureIgnoreCase),
        value => SendMessage($"VOLT:PROT:STAT {(value ? "ON" : "OFF")}"));
    private IAsyncProperty<bool>? _isOverVoltageProtectionEnabled;

    /// <summary>
    ///   Gets the read/write asynchronous property controlling the output over-voltage protection level in volts.
    /// </summary>
    public IAsyncProperty<double> OverVoltageLevel => _overVoltageLevel ??= new AsyncProperty<double>(
      () => double.TryParse(SendMessage("VOLT:PROT?"), NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var value)
        ? value
        : default,
      value => SendMessage($"VOLT:PROT {value}"));
    private IAsyncProperty<double>? _overVoltageLevel;

    /// <summary>
    ///   Gets the read/write asynchronous property controlling the power supply TFT display state.
    ///   <c>true</c> - display enabled, <c>false</c> - display disabled.
    /// </summary>
    public IAsyncProperty<bool> IsDisplayEnabled => _isDisplayEnabled ??= new AsyncProperty<bool>(
      () => TrueBooleanStrings.Contains(SendMessage("DISP?"), StringComparer.InvariantCultureIgnoreCase),
      value => SendMessage($"DISP {(value ? "ON" : "OFF")}"));
    private IAsyncProperty<bool>? _isDisplayEnabled;

    /// <summary>
    ///   Gets the read/write asynchronous property controlling the custom text string displayed on the power supply
    ///   TFT display.
    /// </summary>
    public IAsyncProperty<string> DisplayedText => _displayedText ??= new AsyncProperty<string>(
      () => SendMessage("DISP:TEXT?").Trim('"'),
      value => SendMessage($"DISP:TEXT \"{value}\""));
    private IAsyncProperty<string>? _displayedText;

    /// <summary>
    ///   Checks if the device's interface is <see cref="HardwareInterfaceType.Gpib" /> or
    ///   <see cref="HardwareInterfaceType.Serial" />. Other interfaces are not supported by the power supply.
    /// </summary>
    public override HardwareInterfaceType[] SupportedInterfaces { get; } =
    {
      HardwareInterfaceType.Gpib,
      HardwareInterfaceType.Serial
    };

    /// <inheritdoc />
    /// <exception cref="VisaDeviceException">
    ///   The specified VISA resource is not a Keysight E364xA device.
    /// </exception>
    protected override void Initialize()
    {
      base.Initialize();

      // Serial connection configuration.
      if (Session is ISerialSession serialSession)
      {
        serialSession.BaudRate = 9200;
        serialSession.DataBits = 8;
        serialSession.Parity = SerialParity.None;
        serialSession.StopBits = SerialStopBitsMode.Two;
      }

      if (!Regex.IsMatch(GetIdentifier(), @"E364[0-5]A", RegexOptions.IgnoreCase))
        throw new VisaDeviceException(this,
          new NotSupportedException("The specified VISA resource is not a Keysight E364xA device."));

      if (Session is ISerialSession)
        SetRemoteControl();
    }

    /// <summary>
    ///   Switches the device control scheme to local.
    ///   All front panel keys will be enabled.
    /// </summary>
    [DeviceAction]
    public void SetLocalControl() => SendMessage("SYST:LOC");

    /// <summary>
    ///   Asynchronously switches the device control scheme to local.
    ///   All front panel keys will be enabled.
    /// </summary>
    public Task SetLocalControlAsync() => SendMessageAsync("SYST:LOC");

    /// <summary>
    ///   Switches the device control scheme to remote.
    ///   All front panel keys except the "Local" key will be disabled.
    /// </summary>
    [DeviceAction]
    public void SetRemoteControl() => SendMessage("SYST:REM");

    /// <summary>
    ///   Asynchronously switches the device control scheme to remote.
    ///   All front panel keys except the "Local" key will be disabled.
    /// </summary>
    public Task SetRemoteControlAsync() => SendMessageAsync("SYST:REM");

    /// <summary>
    ///   Switches the device control scheme to remote and locks the front panel.
    ///   All front panel keys will be disabled.
    /// </summary>
    [DeviceAction]
    public void SetControlLock() => SendMessage("SYST:RWL");

    /// <summary>
    ///   Asynchronously switches the device control scheme to remote and locks the front panel.
    ///   All front panel keys will be disabled.
    /// </summary>
    public Task SetControlLockAsync() => SendMessageAsync("SYST:RWL");

    /// <summary>
    ///   Emits a beep.
    /// </summary>
    [DeviceAction]
    public void Beep() => SendMessage("SYST:BEEP");

    /// <summary>
    ///   Asynchronously emits a beep.
    /// </summary>
    public Task BeepAsync() => SendMessageAsync("SYST:BEEP");

    /// <inheritdoc />
    protected override void DeInitialize()
    {
      if (Session is ISerialSession)
        SetLocalControlAsync().Wait();

      base.DeInitialize();
    }
  }
}
