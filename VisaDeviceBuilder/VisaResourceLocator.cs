// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Ivi.Visa;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The static class that helps to locate VISA resources available in the system.
  /// </summary>
  public static class VisaResourceLocator
  {
    /// <summary>
    ///   Locates available VISA resources using the <see cref="GlobalResourceManager" />, and returns the corresponding
    ///   VISA canonical names and alias names if they are available.
    /// </summary>
    /// <param name="pattern">
    ///   An optional pattern string that can be used to filter the returned results.
    ///   Defaults to the pattern that filters VISA resources only of the instrument kind.
    /// </param>
    /// <returns>
    ///   An enumeration of <see cref="VisaResourceName" /> records containing the filtered canonical VISA resource
    ///   names and their corresponding alias names (if available) found by the <see cref="GlobalResourceManager" />.
    /// </returns>
    /// <remarks>
    ///   When using the <see cref="GlobalResourceManager" />, make sure that assemblies for the necessary VISA .NET
    ///   implementations are referenced in the project and can be resolved by the .NET reflection system. Otherwise
    ///   these resources may not be included into the result.
    /// </remarks>
    [ExcludeFromCodeCoverage(Justification = "When using the GlobalResourceManager for VISA resource discovery, " +
      "the returned results may differ significantly in different systems. So the coverage differs as well.")]
    public static Task<IEnumerable<VisaResourceName>> FindVisaResourceNamesAsync(string pattern = "?*::INSTR") =>
      Task.Run(() =>
      {
        try
        {
          return GlobalResourceManager
            .Find(pattern)
            .Aggregate(new List<VisaResourceName>(), (results, resourceName) =>
            {
              var parseResult = GlobalResourceManager.Parse(resourceName);
              results.Add(new VisaResourceName(
                parseResult.OriginalResourceName,
                !string.IsNullOrWhiteSpace(parseResult.AliasIfExists) ? parseResult.AliasIfExists : string.Empty));
              return results;
            })
            .AsEnumerable();
        }
        catch (VisaException)
        {
          return Array.Empty<VisaResourceName>();
        }
      });

    /// <summary>
    ///   Locates available VISA resources using the provided VISA resource manager instance, and returns the
    ///   corresponding VISA canonical names and alias names if they are available.
    /// </summary>
    /// <param name="resourceManager">
    ///   The VISA resource manager instance used for locating VISA resources.
    /// </param>
    /// <param name="pattern">
    ///   An optional pattern string that can be used to filter the returned results.
    ///   Defaults to the pattern that filters VISA resources only of the instrument kind.
    /// </param>
    /// <returns>
    ///   An enumeration of <see cref="VisaResourceName" /> records containing the filtered canonical VISA resource
    ///   names and their corresponding alias names (if available) found by the provided
    ///   <paramref name="resourceManager" /> instance.
    /// </returns>
    public static Task<IEnumerable<VisaResourceName>> FindVisaResourceNamesAsync(IResourceManager resourceManager,
      string pattern = "?*::INSTR") =>
      Task.Run(() =>
      {
        try
        {
          return resourceManager
            .Find(pattern)
            .Aggregate(new List<VisaResourceName>(), (results, resourceName) =>
            {
              var parseResult = resourceManager.Parse(resourceName);
              results.Add(new VisaResourceName(
                parseResult.OriginalResourceName,
                !string.IsNullOrWhiteSpace(parseResult.AliasIfExists) ? parseResult.AliasIfExists : string.Empty));
              return results;
            })
            .AsEnumerable();
        }
        catch (VisaException)
        {
          return Array.Empty<VisaResourceName>();
        }
      });

    /// <summary>
    ///   Locates available VISA resources using the provided VISA resource manager type, and returns the corresponding
    ///   VISA canonical names and alias names if they are available.
    /// </summary>
    /// <param name="resourceManagerType">
    ///   The <see cref="Type" /> of the VISA resource manager used for locating VISA resources.
    /// </param>
    /// <param name="pattern">
    ///   An optional pattern string that can be used to filter the returned results.
    ///   Defaults to the pattern that filters VISA resources only of the instrument kind.
    /// </param>
    /// <returns>
    ///   An enumeration of <see cref="VisaResourceName" /> records containing the filtered canonical VISA resource
    ///   names and their corresponding alias names (if available) found by an internally created instance of
    ///   <paramref name="resourceManagerType" /> type.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   The <paramref name="resourceManagerType" /> value is not a valid VISA resource manager class type.
    /// </exception>
    public static Task<IEnumerable<VisaResourceName>> FindVisaResourceNamesAsync(Type resourceManagerType,
      string pattern = "?*::INSTR") =>
      Task.Run(() =>
      {
        if (!resourceManagerType.IsAssignableTo(typeof(IResourceManager)))
          throw new InvalidOperationException(
            $"\"{resourceManagerType.Name}\" is not a valid VISA resource manager class type.");

        using var resourceManager = (IResourceManager) Activator.CreateInstance(resourceManagerType)!;
        return FindVisaResourceNamesAsync(resourceManager, pattern);
      });

    /// <summary>
    ///   Locates available VISA resources using the provided VISA resource manager type, and returns the corresponding
    ///   VISA canonical names and alias names if they are available.
    /// </summary>
    /// <typeparam name="TResourceManager">
    ///   The type of the VISA resource manager used for locating VISA resources.
    /// </typeparam>
    /// <param name="pattern">
    ///   An optional pattern string that can be used to filter the returned results.
    ///   Defaults to the pattern that filters VISA resources only of the instrument kind.
    /// </param>
    /// <returns>
    ///   An enumeration of <see cref="VisaResourceName" /> records containing the filtered canonical VISA resource
    ///   names and their corresponding alias names (if available) found by an internally created instance of
    ///   <typeparamref name="TResourceManager" /> type.
    /// </returns>
    public static Task<IEnumerable<VisaResourceName>> FindVisaResourceNamesAsync<TResourceManager>(
      string pattern = "?*::INSTR") where TResourceManager : IResourceManager =>
      FindVisaResourceNamesAsync(typeof(TResourceManager), pattern);
  }
}
