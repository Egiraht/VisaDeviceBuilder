using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ivi.Visa;

// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo
namespace VisaDeviceBuilder.SampleApp.Components
{
  /// <summary>
  ///   The VISA device class representing a Keysight E364xA series power supply.
  /// </summary>
  public class KeysightE364xA : KeysightScpiDevice
  {
    /* The backing fields for the device asynchronous properties. */
    #region Backing fields
    private IAsyncProperty<bool>? _isOutputEnabled;
    private IAsyncProperty<double>? _targetVoltage;
    private IAsyncProperty<double>? _currentLimit;
    private IAsyncProperty<double>? _measuredVoltage;
    private IAsyncProperty<double>? _measuredCurrent;
    private IAsyncProperty<bool>? _isOverVoltageProtectionEnabled;
    private IAsyncProperty<double>? _overVoltageLevel;
    private IAsyncProperty<bool>? _isDisplayEnabled;
    private IAsyncProperty<string>? _displayedText;
    #endregion

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

    /// <summary>
    ///   Gets the read/write asynchronous property controlling the target output voltage in volts.
    /// </summary>
    public IAsyncProperty<double> TargetVoltage => _targetVoltage ??= new AsyncProperty<double>(
      () => SendMessage("VOLT?"),
      value => SendMessage($"VOLT {value}"));

    /// <summary>
    ///   Gets the read/write asynchronous property controlling the output current limit in amps.
    /// </summary>
    public IAsyncProperty<double> CurrentLimit => _currentLimit ??= new AsyncProperty<double>(
      () => SendMessage("CURR?"),
      value => SendMessage($"CURR {value}"));

    /// <summary>
    ///   Gets the read-only asynchronous property accessing the measured output voltage.
    /// </summary>
    public IAsyncProperty<double> MeasuredVoltage => _measuredVoltage ??= new AsyncProperty<double>(
      () => SendMessage("MEAS:VOLT?"));

    /// <summary>
    ///   Gets the read-only asynchronous property accessing the measured output current.
    /// </summary>
    public IAsyncProperty<double> MeasuredCurrent => _measuredCurrent ??= new AsyncProperty<double>(
      () => SendMessage("MEAS:CURR?"));

    /// <summary>
    ///   Gets the read/write asynchronous property controlling the output over-voltage protection (OVP) state.
    ///   <c>true</c> - OVP enabled, <c>false</c> - OVP disabled.
    /// </summary>
    public IAsyncProperty<bool> IsOverVoltageProtectionEnabled => _isOverVoltageProtectionEnabled ??=
      new AsyncProperty<bool>(
        () => TrueBooleanStrings.Contains(SendMessage("VOLT:PROT:STAT?"), StringComparer.InvariantCultureIgnoreCase),
        value => SendMessage($"VOLT:PROT:STAT {(value ? "ON" : "OFF")}"));

    /// <summary>
    ///   Gets the read/write asynchronous property controlling the output over-voltage protection level in volts.
    /// </summary>
    public IAsyncProperty<double> OverVoltageLevel => _overVoltageLevel ??= new AsyncProperty<double>(
      () => SendMessage("VOLT:PROT?"),
      value => SendMessage($"VOLT:PROT {value}"));

    /// <summary>
    ///   Gets the read/write asynchronous property controlling the power supply TFT display state.
    ///   <c>true</c> - display enabled, <c>false</c> - display disabled.
    /// </summary>
    public IAsyncProperty<bool> IsDisplayEnabled => _isDisplayEnabled ??= new AsyncProperty<bool>(
      () => TrueBooleanStrings.Contains(SendMessage("DISP?"), StringComparer.InvariantCultureIgnoreCase),
      value => SendMessage($"DISP {(value ? "ON" : "OFF")}"));

    /// <summary>
    ///   Gets the read/write asynchronous property controlling the custom text string displayed on the power supply
    ///   TFT display.
    /// </summary>
    public IAsyncProperty<string> DisplayedText => _displayedText ??= new AsyncProperty<string>(
      () => SendMessage("DISP:TEXT?").Trim('"'),
      value => SendMessage($"DISP:TEXT \"{value}\""));

    /// <summary>
    ///   Checks if the device's interface is <see cref="HardwareInterfaceType.Gpib" /> or
    ///   <see cref="HardwareInterfaceType.Serial" />. Other interfaces are not supported by the power supply.
    /// </summary>
    public override HardwareInterfaceType[] SupportedInterfaces { get; } =
    {
      HardwareInterfaceType.Gpib,
      HardwareInterfaceType.Serial
    };

    /// <summary>
    ///   Creates a new instance of the <see cref="KeysightE364xA" /> class.
    /// </summary>
    /// <param name="resourceName">
    ///   The VISA resource name of the device.
    /// </param>
    /// <param name="resourceManager">
    ///   The custom VISA resource manager instance used for VISA session management.
    ///   If set to <c>null</c>, the <see cref="GlobalResourceManager" /> static class will be used.
    /// </param>
    public KeysightE364xA(string resourceName, IResourceManager? resourceManager = null) :
      base(resourceName, resourceManager)
    {
    }

    /// <inheritdoc />
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
