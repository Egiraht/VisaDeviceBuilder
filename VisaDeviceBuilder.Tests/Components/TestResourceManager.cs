using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Ivi.Visa;
using Moq;

namespace VisaDeviceBuilder.Tests.Components
{
  /// <summary>
  ///   The custom VISA resource manager class used for <see cref="VisaDevice" /> class testing purposes.
  /// </summary>
  [ExcludeFromCodeCoverage]
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

    /// <inheritdoc />
    public Version ImplementationVersion { get; } = GlobalResourceManager.ImplementationVersion;

    /// <inheritdoc />
    public Version SpecificationVersion { get; } = GlobalResourceManager.SpecificationVersion;

    /// <inheritdoc />
    public string ManufacturerName { get; } = nameof(VisaDeviceBuilder);

    /// <inheritdoc />
    public short ManufacturerId { get; } = 0x1234;

    /// <inheritdoc />
    public IEnumerable<string> Find(string pattern) => new[]
    {
      CustomTestDeviceResourceName,
      SerialTestDeviceResourceName,
      VxiTestDeviceResourceName
    };

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
        case {InterfaceType: SerialTestDeviceInterfaceType}:
        {
          var mock = new Mock<IMessageBasedSession>();
          mock.SetupProperty(session => session.TimeoutMilliseconds, timeoutMilliseconds);
          mock.Setup(session => session.FormattedIO.WriteLine(string.Empty));
          mock.Setup(session => session.FormattedIO.ReadLine()).Returns(string.Empty);
          openStatus = ResourceOpenStatus.Success;
          return mock.Object;
        }

        case {InterfaceType: VxiTestDeviceInterfaceType}:
        {
          var mock = new Mock<IRegisterBasedSession>();
          mock.SetupProperty(session => session.TimeoutMilliseconds, timeoutMilliseconds);
          openStatus = ResourceOpenStatus.Success;
          return mock.Object;
        }

        default:
        {
          var mock = new Mock<IVisaSession>();
          mock.SetupProperty(session => session.TimeoutMilliseconds, timeoutMilliseconds);
          openStatus = ResourceOpenStatus.Success;
          return mock.Object;
        }
      }
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
  }
}
