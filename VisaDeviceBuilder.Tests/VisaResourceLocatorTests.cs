// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Linq;
using System.Threading.Tasks;
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
    ///   Testing discovery of available VISA resources using the <see cref="TestResourceManager" /> class.
    /// </summary>
    [Fact]
    public async Task TestResourceManagerDiscoveryTest()
    {
      // All test VISA resource names and their alias names provided by the TestResourceManager class must be found.
      var resources = (await VisaResourceLocator.FindVisaResourceNamesAsync<TestResourceManager>()).ToArray();
      Assert.Collection(resources,
        visaResourceName => Assert.True(visaResourceName is
          {
            CanonicalName: TestResourceManager.CustomTestDeviceResourceName,
            AliasName: TestResourceManager.CustomTestDeviceAliasName
          } name
          && name == TestResourceManager.CustomTestDeviceResourceName
          && name.ToString().Contains(TestResourceManager.CustomTestDeviceAliasName)
          && name.ToString().Contains(TestResourceManager.CustomTestDeviceResourceName)),
        visaResourceName => Assert.True(visaResourceName is
          {
            CanonicalName: TestResourceManager.SerialTestDeviceResourceName,
            AliasName: TestResourceManager.SerialTestDeviceAliasName
          } name
          && name == TestResourceManager.SerialTestDeviceResourceName
          && name.ToString().Contains(TestResourceManager.SerialTestDeviceAliasName)
          && name.ToString().Contains(TestResourceManager.SerialTestDeviceResourceName)),
        visaResourceName => Assert.True(visaResourceName is
          {
            CanonicalName: TestResourceManager.VxiTestDeviceResourceName,
            AliasName: TestResourceManager.VxiTestDeviceAliasName
          } name
          && name == TestResourceManager.VxiTestDeviceResourceName
          && name.ToString().Contains(TestResourceManager.VxiTestDeviceAliasName)
          && name.ToString().Contains(TestResourceManager.VxiTestDeviceResourceName)));
    }

    /// <summary>
    ///   Testing no VISA resources processing.
    /// </summary>
    [Fact]
    public async Task NoResourcesTest()
    {
      // When no matching resources are found, the method must return an empty enumeration without any exceptions.
      Assert.Empty(await VisaResourceLocator.FindVisaResourceNamesAsync<TestResourceManager>(
        TestResourceManager.NoResourcesPattern));
    }

    /// <summary>
    ///   Testing an invalid VISA resource manager class.
    /// </summary>
    [Fact]
    public async Task InvalidResourceManagerTypeTest() =>
      await Assert.ThrowsAsync<InvalidOperationException>(() =>
        VisaResourceLocator.FindVisaResourceNamesAsync(typeof(TestMessageDevice)));
  }
}
