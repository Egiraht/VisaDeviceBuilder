// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Ivi.Visa;
using Moq;

namespace VisaDeviceBuilder.Tests.Components
{
  /// <summary>
  ///   The custom VISA resource manager class used for <see cref="VisaDevice" /> class testing purposes.
  /// </summary>
  public class TestResourceManager : IResourceManager
  {
    /// <summary>
    ///   Defines the default access mode for VISA sessions.
    /// </summary>
    public const AccessModes DefaultAccessMode = AccessModes.ExclusiveLock;

    /// <summary>
    ///   Defines the default connection timeout in milliseconds.
    /// </summary>
    public const int DefaultConnectionTimeout = 1000;

    /// <summary>
    ///   Defines the session opening delay in milliseconds.
    /// </summary>
    public const int SessionOpeningDelay = 1;

    /// <summary>
    ///   Defines the custom test VISA device resource name.
    /// </summary>
    public const string CustomTestDeviceResourceName = "CUSTOM::INSTR";

    /// <summary>
    ///   Defines the custom test VISA device alias name.
    /// </summary>
    public const string CustomTestDeviceAliasName = "CustomTestDevice";

    /// <summary>
    ///   Defines the custom test VISA device interface type.
    /// </summary>
    public const HardwareInterfaceType CustomTestDeviceInterfaceType = HardwareInterfaceType.Custom;

    /// <summary>
    ///   Defines the custom test VISA device interface number.
    /// </summary>
    public const int CustomTestDeviceInterfaceNumber = 0;

    /// <summary>
    ///   Defines the custom test VISA device resource class.
    /// </summary>
    public const string CustomTestDeviceResourceClass = "CUSTOM";

    /// <summary>
    ///   Defines the serial test VISA device resource name.
    /// </summary>
    public const string SerialTestDeviceResourceName = "SERIAL::INSTR";

    /// <summary>
    ///   Defines the serial test VISA device alias name.
    /// </summary>
    public const string SerialTestDeviceAliasName = "SerialTestDevice";

    /// <summary>
    ///   Defines the serial test VISA device interface type.
    /// </summary>
    public const HardwareInterfaceType SerialTestDeviceInterfaceType = HardwareInterfaceType.Serial;

    /// <summary>
    ///   Defines the serial test VISA device interface number.
    /// </summary>
    public const int SerialTestDeviceInterfaceNumber = 0;

    /// <summary>
    ///   Defines the serial test VISA device resource class.
    /// </summary>
    public const string SerialTestDeviceResourceClass = "SERIAL";

    /// <summary>
    ///   Defines the VXI test VISA device resource name.
    /// </summary>
    public const string VxiTestDeviceResourceName = "VXI::INSTR";

    /// <summary>
    ///   Defines the VXI test VISA device alias name.
    /// </summary>
    public const string VxiTestDeviceAliasName = "VxiTestDevice";

    /// <summary>
    ///   Defines the VXI test VISA device interface type.
    /// </summary>
    public const HardwareInterfaceType VxiTestDeviceInterfaceType = HardwareInterfaceType.Vxi;

    /// <summary>
    ///   Defines the VXI test VISA device interface number.
    /// </summary>
    public const int VxiTestDeviceInterfaceNumber = 0;

    /// <summary>
    ///   Defines the VXI test VISA device resource class.
    /// </summary>
    public const string VxiTestDeviceResourceClass = "VXI";

    /// <summary>
    ///   Defines the pattern string that instructs the test resource manager to throw an exception that no suitable
    ///   VISA resources were found.
    /// </summary>
    public const string NoResourcesPattern = "EMPTY";

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public Version ImplementationVersion => GlobalResourceManager.ImplementationVersion;

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public Version SpecificationVersion => GlobalResourceManager.SpecificationVersion;

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public string ManufacturerName => nameof(VisaDeviceBuilder);

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public short ManufacturerId => 0x1234;

    /// <summary>
    ///   Gets the messages queue that represents a test input-output buffer of a message-based session.
    /// </summary>
    public Queue<string> Message { get; } = new();

    /// <summary>
    ///   Checks if this resource manager instance has been already disposed of.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc />
    public IEnumerable<string> Find(string pattern = "")
    {
      if (pattern == NoResourcesPattern)
        throw new VisaException("Failed to find any resources matching the pattern \"{pattern}\".");

      return new[]
      {
        CustomTestDeviceResourceName,
        SerialTestDeviceResourceName,
        VxiTestDeviceResourceName
      };
    }

    /// <inheritdoc />
    public ParseResult Parse(string resourceName)
    {
      return resourceName switch
      {
        CustomTestDeviceResourceName => new ParseResult(resourceName, CustomTestDeviceInterfaceType,
          CustomTestDeviceInterfaceNumber, CustomTestDeviceResourceClass, CustomTestDeviceResourceName,
          CustomTestDeviceAliasName),
        SerialTestDeviceResourceName => new ParseResult(resourceName, SerialTestDeviceInterfaceType,
          SerialTestDeviceInterfaceNumber, SerialTestDeviceResourceClass, SerialTestDeviceResourceName,
          SerialTestDeviceAliasName),
        VxiTestDeviceResourceName => new ParseResult(resourceName, VxiTestDeviceInterfaceType,
          VxiTestDeviceInterfaceNumber, VxiTestDeviceResourceClass, VxiTestDeviceResourceName,
          VxiTestDeviceAliasName),
        _ => throw new VisaException($"Failed to parse {resourceName}")
      };
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public IVisaSession Open(string resourceName) => Open(resourceName, DefaultAccessMode, DefaultConnectionTimeout);

    /// <inheritdoc />
    public IVisaSession Open(string resourceName, AccessModes accessModes, int timeoutMilliseconds) =>
      Open(resourceName, accessModes, timeoutMilliseconds, out _);

    /// <inheritdoc />
    public IVisaSession Open(string resourceName, AccessModes accessModes, int timeoutMilliseconds,
      out ResourceOpenStatus openStatus)
    {
      var parseResult = Parse(resourceName);
      switch (parseResult)
      {
        case { InterfaceType: SerialTestDeviceInterfaceType }:
        {
          var mock = new Mock<ISerialSession> { Name = nameof(ISerialSession) };
          mock.SetupGet(session => session.ResourceName)
            .Returns(resourceName);
          mock.SetupProperty(session => session.TimeoutMilliseconds, timeoutMilliseconds);
          mock.SetupProperty(session => session.BaudRate, TestMessageDevice.TestSerialConfiguration.BaudRate);
          mock.SetupProperty(session => session.DataBits, TestMessageDevice.TestSerialConfiguration.DataBits);
          mock.SetupProperty(session => session.Parity, TestMessageDevice.TestSerialConfiguration.Parity);
          mock.SetupProperty(session => session.StopBits, TestMessageDevice.TestSerialConfiguration.StopBits);
          mock.SetupProperty(session => session.FlowControl, TestMessageDevice.TestSerialConfiguration.FlowControl);
          mock.SetupProperty(session => session.DataTerminalReadyState,
            TestMessageDevice.TestSerialConfiguration.DataTerminalReadyState);
          mock.SetupProperty(session => session.RequestToSendState,
            TestMessageDevice.TestSerialConfiguration.RequestToSendState);
          mock.SetupProperty(session => session.ReadTermination,
            TestMessageDevice.TestSerialConfiguration.ReadTermination);
          mock.SetupProperty(session => session.WriteTermination,
            TestMessageDevice.TestSerialConfiguration.WriteTermination);
          mock.SetupProperty(session => session.ReplacementCharacter,
            TestMessageDevice.TestSerialConfiguration.ReplacementCharacter);
          mock.SetupProperty(session => session.XOffCharacter, TestMessageDevice.TestSerialConfiguration.XOffCharacter);
          mock.SetupProperty(session => session.XOnCharacter, TestMessageDevice.TestSerialConfiguration.XOnCharacter);
          mock.Setup(session => session.FormattedIO.WriteLine(It.IsAny<string>()))
            .Callback((string message) => Message.Enqueue(message));
          mock.Setup(session => session.FormattedIO.ReadLine())
            .Returns(() => Message.TryDequeue(out var str) ? str : string.Empty);

          Task.Delay(SessionOpeningDelay).Wait();
          openStatus = ResourceOpenStatus.Success;
          return mock.Object;
        }

        case { InterfaceType: VxiTestDeviceInterfaceType }:
        {
          var mock = new Mock<IRegisterBasedSession> { Name = nameof(IRegisterBasedSession) };
          mock.SetupGet(session => session.ResourceName)
            .Returns(resourceName);
          mock.SetupProperty(session => session.TimeoutMilliseconds, timeoutMilliseconds);

          Task.Delay(SessionOpeningDelay).Wait();
          openStatus = ResourceOpenStatus.Success;
          return mock.Object;
        }

        default:
        {
          var mock = new Mock<IVisaSession> { Name = nameof(IVisaSession) };
          mock.SetupGet(session => session.ResourceName)
            .Returns(resourceName);
          mock.SetupProperty(session => session.TimeoutMilliseconds, timeoutMilliseconds);

          Task.Delay(SessionOpeningDelay).Wait();
          openStatus = ResourceOpenStatus.Success;
          return mock.Object;
        }
      }
    }

    /// <inheritdoc />
    public void Dispose() => IsDisposed = true;
  }
}
