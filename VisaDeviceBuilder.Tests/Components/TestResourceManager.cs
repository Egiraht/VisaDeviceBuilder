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
    public const int SessionOpeningDelay = 50;

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
    public IEnumerable<string> Find(string pattern = "") => new[]
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
        case {InterfaceType: SerialTestDeviceInterfaceType}:
        {
          var mock = new Mock<IMessageBasedSession> {Name = nameof(IMessageBasedSession)};
          mock.SetupGet(session => session.ResourceName)
            .Returns(resourceName);
          mock.SetupProperty(session => session.TimeoutMilliseconds, timeoutMilliseconds);
          mock.Setup(session => session.FormattedIO.WriteLine(It.IsAny<string>()))
            .Callback((string message) => Message.Enqueue(message));
          mock.Setup(session => session.FormattedIO.ReadLine())
            .Returns(() => Message.TryDequeue(out var str) ? str : string.Empty);

          Task.Delay(SessionOpeningDelay).Wait();
          openStatus = ResourceOpenStatus.Success;
          return mock.Object;
        }

        case {InterfaceType: VxiTestDeviceInterfaceType}:
        {
          var mock = new Mock<IRegisterBasedSession> {Name = nameof(IRegisterBasedSession)};
          mock.SetupGet(session => session.ResourceName)
            .Returns(resourceName);
          mock.SetupProperty(session => session.TimeoutMilliseconds, timeoutMilliseconds);

          Task.Delay(SessionOpeningDelay).Wait();
          openStatus = ResourceOpenStatus.Success;
          return mock.Object;
        }

        default:
        {
          var mock = new Mock<IVisaSession> {Name = nameof(IVisaSession)};
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
