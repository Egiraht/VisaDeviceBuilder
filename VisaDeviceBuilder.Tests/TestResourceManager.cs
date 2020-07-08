using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Ivi.Visa;
using Moq;

namespace VisaDeviceBuilder.Tests
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
    ///   Defines the test VISA device resource name.
    /// </summary>
    public const string TestDeviceResourceName = "TEST::INSTR";

    /// <summary>
    ///   Defines the test VISA device alias name.
    /// </summary>
    public const string TestDeviceAliasName = "TestDevice";

    /// <summary>
    ///   Defines the test VISA device interface type.
    /// </summary>
    public const HardwareInterfaceType TestDeviceInterfaceType = HardwareInterfaceType.Serial;

    /// <summary>
    ///   Defines the test VISA device interface number.
    /// </summary>
    public const int TestDeviceInterfaceNumber = 0;

    /// <summary>
    ///   Defines the test VISA device resource class.
    /// </summary>
    public const string TestDeviceResourceClass = "TEST";

    /// <inheritdoc />
    public Version ImplementationVersion { get; } = GlobalResourceManager.ImplementationVersion;

    /// <inheritdoc />
    public Version SpecificationVersion { get; } = GlobalResourceManager.SpecificationVersion;

    /// <inheritdoc />
    public string ManufacturerName { get; } = nameof(VisaDeviceBuilder);

    /// <inheritdoc />
    public short ManufacturerId { get; } = 0x1234;

    /// <inheritdoc />
    public IEnumerable<string> Find(string pattern) => new[] {TestDeviceResourceName};

    /// <inheritdoc />
    public ParseResult Parse(string resourceName) => new ParseResult(resourceName, TestDeviceInterfaceType,
      TestDeviceInterfaceNumber, TestDeviceResourceClass, TestDeviceResourceName, TestDeviceAliasName);

    /// <inheritdoc />
    public IVisaSession Open(string resourceName) => Open(resourceName, DefaultAccessMode, DefaultConnectionTimeout);

    /// <inheritdoc />
    public IVisaSession Open(string resourceName, AccessModes accessModes, int timeoutMilliseconds) =>
      Open(resourceName, accessModes, timeoutMilliseconds, out _);

    /// <inheritdoc />
    public IVisaSession Open(string resourceName, AccessModes accessModes, int timeoutMilliseconds,
      out ResourceOpenStatus openStatus)
    {
      var mock = new Mock<IVisaSession>();
      mock.SetupProperty(session => session.TimeoutMilliseconds, timeoutMilliseconds);
      openStatus = ResourceOpenStatus.Success;
      return mock.Object;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
  }
}
