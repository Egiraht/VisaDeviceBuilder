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
      var resources = (await VisaResourceLocator.LocateResourceNamesAsync<TestResourceManager>()).ToArray();
      Assert.Contains(TestResourceManager.CustomTestDeviceResourceName, resources);
      Assert.Contains(TestResourceManager.CustomTestDeviceAliasName, resources);
      Assert.Contains(TestResourceManager.SerialTestDeviceResourceName, resources);
      Assert.Contains(TestResourceManager.SerialTestDeviceAliasName, resources);
      Assert.Contains(TestResourceManager.VxiTestDeviceResourceName, resources);
      Assert.Contains(TestResourceManager.VxiTestDeviceAliasName, resources);
    }

    /// <summary>
    ///   Testing no VISA resources processing.
    /// </summary>
    [Fact]
    public async Task NoResourcesTest()
    {
      // When no matching resources are found, the method must return an empty enumeration without any exceptions.
      Assert.Empty(await VisaResourceLocator.LocateResourceNamesAsync<TestResourceManager>(
        TestResourceManager.NoResourcesPattern));
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
