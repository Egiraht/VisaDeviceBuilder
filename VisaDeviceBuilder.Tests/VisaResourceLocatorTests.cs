using System;
using System.Linq;
using System.Threading.Tasks;
using Ivi.Visa;
using VisaDeviceBuilder.Tests.Components;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="VisaResourceLocator" /> class.
  /// </summary>
  public class VisaResourceLocatorTests
  {
    /// <summary>
    ///   Testing discovery of available VISA resources using the <see cref="GlobalResourceManager" /> class.
    /// </summary>
    [Fact]
    public async Task GlobalResourceManagerDiscoveryTest()
    {
      // Found VISA resources must be parsable by the GlobalResourceManager.
      var resources = await VisaResourceLocator.LocateResourceNamesAsync();
      foreach (var resource in resources)
        Assert.True(GlobalResourceManager.TryParse(resource, out _));
    }

    /// <summary>
    ///   Testing discovery of available VISA resources using the <see cref="TestResourceManager" /> class.
    /// </summary>
    [Fact]
    public async Task TestResourceManagerDiscoveryTest()
    {
      var resources = (await VisaResourceLocator.LocateResourceNamesAsync<TestResourceManager>()).ToArray();
      Assert.Contains(TestResourceManager.CustomTestDeviceResourceName, resources);
      Assert.Contains(TestResourceManager.CustomTestDeviceAliasName, resources);
      Assert.Contains(TestResourceManager.SerialTestDeviceResourceName, resources);
      Assert.Contains(TestResourceManager.SerialTestDeviceAliasName, resources);
      Assert.Contains(TestResourceManager.VxiTestDeviceResourceName, resources);
      Assert.Contains(TestResourceManager.VxiTestDeviceAliasName, resources);
    }

    /// <summary>
    ///   Testing an invalid VISA resource manager class.
    /// </summary>
    [Fact]
    public async Task InvalidResourceManagerTypeTest() =>
      await Assert.ThrowsAsync<InvalidOperationException>(() =>
        VisaResourceLocator.LocateResourceNamesAsync(typeof(TestMessageDevice)));
  }
}
