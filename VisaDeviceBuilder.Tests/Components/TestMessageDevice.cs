// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System.Threading.Tasks;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder.Tests.Components
{
  /// <summary>
  ///   The test message-based VISA device used for testing purposes.
  /// </summary>
  public class TestMessageDevice : MessageDevice
  {
    /// <summary>
    ///   Defines the communication delay in milliseconds imitated by this test device.
    /// </summary>
    public const int CommunicationDelay = 1;

    /// <summary>
    ///   Gets the test serial configuration object.
    /// </summary>
    public static readonly SerialConfiguration TestSerialConfiguration = new()
    {
      BaudRate = 115200,
      DataBits = 7,
      Parity = SerialParity.Even,
      StopBits = SerialStopBitsMode.Two,
      FlowControl = SerialFlowControlModes.DtrDsr,
      DataTerminalReadyState = LineState.Unasserted,
      RequestToSendState = LineState.Unknown,
      ReadTermination = SerialTerminationMethod.Break,
      WriteTermination = SerialTerminationMethod.HighestBit,
      ReplacementCharacter = 0x0A,
      XOffCharacter = 0x01,
      XOnCharacter = 0x02
    };

    /// <summary>
    ///   The synchronization locking object.
    /// </summary>
    private readonly object _synchronizationLock = new();

    /// <summary>
    ///   Gets or sets the actual value accessed by the <see cref="TestAsyncProperty" /> property.
    /// </summary>
    public int TestValue { get; set; }

    /// <summary>
    ///   Gets or sets the flag indicating if the <see cref="TestDeviceAction" /> has been called.
    /// </summary>
    public bool IsTestDeviceActionCalled { get; set; }

    /// <summary>
    ///   Gets or sets the flag indicating if the <see cref="IVisaDevice.Reset" /> device action has been called.
    /// </summary>
    public bool IsResetCalled { get; set; }

    /// <summary>
    ///   Gets the test asynchronous property of integer type that is defined using the <see cref="IAsyncProperty" />
    ///   type class declaration and must be enlisted into the <see cref="IVisaDevice.AsyncProperties" /> enumeration.
    /// </summary>
    public IAsyncProperty<int> TestAsyncProperty => _testAsyncProperty ??= new AsyncProperty<int>(_ =>
    {
      lock (_synchronizationLock)
      {
        if (ThrowOnAsyncPropertyGetter)
          throw new TestException();

        Task.Delay(CommunicationDelay).Wait();
        return TestValue;
      }
    }, (_, newValue) =>
    {
      lock (_synchronizationLock)
      {
        if (ThrowOnAsyncPropertySetter)
          throw new TestException();

        Task.Delay(CommunicationDelay).Wait();
        TestValue = newValue;
      }
    });
    private IAsyncProperty<int>? _testAsyncProperty;

    /// <summary>
    ///   Gets the device action that is defined using the <see cref="IDeviceAction" /> type class declaration and
    ///   must be enlisted into the <see cref="IVisaDevice.DeviceActions" /> enumeration.
    /// </summary>
    public IDeviceAction TestDeviceAction => _testDeviceAction ??= new DeviceAction(_ =>
    {
      Task.Delay(CommunicationDelay).Wait();
      IsTestDeviceActionCalled = true;
    });
    private IDeviceAction? _testDeviceAction;

    /// <summary>
    ///   Gets or sets the flag defining if a test exception should be thrown on the device initialization.
    /// </summary>
    public bool ThrowOnInitialization { get; set; }

    /// <summary>
    ///   Gets or sets the flag defining if a test exception should be thrown on the device de-initialization.
    /// </summary>
    public bool ThrowOnDeInitialization { get; set; }

    /// <summary>
    ///   Gets or sets the flag defining if a test exception should be thrown during <see cref="TestAsyncProperty" />
    ///   getter updating.
    /// </summary>
    public bool ThrowOnAsyncPropertyGetter { get; set; }

    /// <summary>
    ///   Gets or sets the flag defining if a test exception should be thrown during <see cref="TestAsyncProperty" />
    ///   setter processing.
    /// </summary>
    public bool ThrowOnAsyncPropertySetter { get; set; }

    /// <inheritdoc />
    protected override void DefaultInitializeCallback()
    {
      if (ThrowOnInitialization)
        throw new TestException();
    }

    /// <inheritdoc />
    protected override void DefaultDeInitializeCallback()
    {
      if (ThrowOnDeInitialization)
        throw new TestException();
    }

    /// <inheritdoc />
    protected override void DefaultResetCallback()
    {
      Task.Delay(CommunicationDelay).Wait();
      IsResetCalled = true;
    }
  }
}
