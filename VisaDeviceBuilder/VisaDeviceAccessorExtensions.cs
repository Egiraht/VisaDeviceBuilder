// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin <stibiu@yandex.ru>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The static class containing extension methods for objects implementing the <see cref="IVisaDeviceAccessor" />
  ///   interface.
  /// </summary>
  public static class VisaDeviceAccessorExtensions
  {
    /// <summary>
    ///   Searches the object with the attached VISA device instance for all asynchronous properties matching only the
    ///   provided name (regardless of value type).
    /// </summary>
    /// <param name="visaDeviceAccessor">
    ///   The object to be searched.
    /// </param>
    /// <param name="name">
    ///   The asynchronous property name to search for. The name is not case-sensitive.
    /// </param>
    /// <returns>
    ///   An enumeration of all found asynchronous properties matching the provided name.
    /// </returns>
    public static IEnumerable<IAsyncProperty> FindAsyncProperties(this IVisaDeviceAccessor visaDeviceAccessor,
      string name) =>
      visaDeviceAccessor.Device.AsyncProperties
        .Where(asyncProperty => asyncProperty.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

    /// <summary>
    ///   Searches the object with the attached VISA device instance for all asynchronous properties matching the
    ///   provided name and value type.
    /// </summary>
    /// <typeparam name="TValue">
    ///   The value type the asynchronous property must match.
    /// </typeparam>
    /// <param name="visaDeviceAccessor">
    ///   The object to be searched.
    /// </param>
    /// <param name="name">
    ///   The asynchronous property name to search for. The name is not case-sensitive.
    /// </param>
    /// <returns>
    ///   An enumeration of all found asynchronous properties matching the provided name and value type.
    /// </returns>
    public static IEnumerable<IAsyncProperty<TValue>> FindAsyncProperties<TValue>(
      this IVisaDeviceAccessor visaDeviceAccessor, string name) =>
      visaDeviceAccessor
        .FindAsyncProperties(name)
        .Where(asyncProperty => asyncProperty is IAsyncProperty<TValue>)
        .Cast<IAsyncProperty<TValue>>();

    /// <summary>
    ///   Searches the object with the attached VISA device instance for the last asynchronous property matching only
    ///   the provided name (regardless of value type).
    /// </summary>
    /// <param name="visaDeviceAccessor">
    ///   The object to be searched.
    /// </param>
    /// <param name="name">
    ///   The asynchronous property name to search for. The name is not case-sensitive.
    /// </param>
    /// <returns>
    ///   The last found asynchronous property matching the provided name, or <c>null</c> otherwise.
    /// </returns>
    public static IAsyncProperty? FindAsyncProperty(this IVisaDeviceAccessor visaDeviceAccessor, string name) =>
      visaDeviceAccessor.FindAsyncProperties(name).LastOrDefault();

    /// <summary>
    ///   Searches the object with the attached VISA device instance for the last asynchronous property matching the
    ///   provided name and value type.
    /// </summary>
    /// <typeparam name="TValue">
    ///   The value type the asynchronous property must match.
    /// </typeparam>
    /// <param name="visaDeviceAccessor">
    ///   The object to be searched.
    /// </param>
    /// <param name="name">
    ///   The asynchronous property name to search for. The name is not case-sensitive.
    /// </param>
    /// <returns>
    ///   The last found asynchronous property matching the provided name and value type, or <c>null</c> otherwise.
    /// </returns>
    public static IAsyncProperty<TValue>? FindAsyncProperty<TValue>(this IVisaDeviceAccessor visaDeviceAccessor,
      string name) =>
      visaDeviceAccessor.FindAsyncProperties<TValue>(name).LastOrDefault();

    /// <summary>
    ///   Searches the object with the attached VISA device instance for the last asynchronous property matching the
    ///   provided name and value type, and returns its last updated value if it is readable.
    /// </summary>
    /// <typeparam name="TValue">
    ///   The value type the asynchronous property must match.
    /// </typeparam>
    /// <param name="visaDeviceAccessor">
    ///   The object to be searched.
    /// </param>
    /// <param name="name">
    ///   The asynchronous property name to search for. The name is not case-sensitive.
    /// </param>
    /// <param name="defaultValue">
    ///   The default value that will be returned if no matching asynchronous properties are found.
    /// </param>
    /// <returns>
    ///   The last updated value of the last found asynchronous property matching the provided name and value type, or
    ///   the <paramref name="defaultValue" /> otherwise. The <paramref name="defaultValue" /> is also returned if the
    ///   matching asynchronous property is write-only.
    /// </returns>
    /// <remarks>
    ///   This method does not invoke asynchronous property getter update and just returns the last updated getter
    ///   value stored in the corresponding asynchronous property.
    /// </remarks>
    public static TValue GetAsyncPropertyValue<TValue>(this IVisaDeviceAccessor visaDeviceAccessor, string name,
      TValue defaultValue = default!) =>
      visaDeviceAccessor
          .FindAsyncProperties<TValue>(name)
          .LastOrDefault(asyncProperty => asyncProperty is { CanGet: true })
        is { } matchedAsyncProperty
        ? matchedAsyncProperty.Getter
        : defaultValue;

    /// <summary>
    ///   Searches the object with the attached VISA device instance for the last asynchronous property matching the
    ///   provided name and value type, and sets its new value if it is writable.
    /// </summary>
    /// <typeparam name="TValue">
    ///   The value type the asynchronous property must match.
    /// </typeparam>
    /// <param name="visaDeviceAccessor">
    ///   The object to be searched.
    /// </param>
    /// <param name="name">
    ///   The asynchronous property name to search for. The name is not case-sensitive.
    /// </param>
    /// <param name="value">
    ///   The new value to be set for the asynchronous property.
    /// </param>
    /// <returns>
    ///   <c>true</c> if the new value has been assigned for the last found matching asynchronous property, otherwise
    ///   <c>false</c>. <c>false</c> is also returned if the matching asynchronous property is read-only.
    /// </returns>
    /// <remarks>
    ///   This method does not throw any exceptions. All exceptions caught during the asynchronous property setter
    ///   value processing can be addressed through subscribing to the <see cref="IAsyncProperty.SetterException" />
    ///   event of the corresponding asynchronous property instance.
    /// </remarks>
    public static bool SetAsyncPropertyValue<TValue>(this IVisaDeviceAccessor visaDeviceAccessor, string name,
      TValue value)
    {
      if (visaDeviceAccessor.FindAsyncProperty<TValue>(name) is not { CanSet: true } asyncProperty)
        return false;
      asyncProperty.Setter = value;
      return true;
    }

    /// <summary>
    ///   Searches the object with the attached VISA device instance for all device actions matching the provided name.
    /// </summary>
    /// <param name="visaDeviceAccessor">
    ///   The object to be searched.
    /// </param>
    /// <param name="name">
    ///   The device action name to search for. The name is not case-sensitive.
    /// </param>
    /// <returns>
    ///   An enumeration of all found device actions matching the provided name.
    /// </returns>
    public static IEnumerable<IDeviceAction> FindDeviceActions(this IVisaDeviceAccessor visaDeviceAccessor,
      string name) =>
      visaDeviceAccessor.Device.DeviceActions
        .Where(deviceAction => deviceAction.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

    /// <summary>
    ///   Searches the object with the attached VISA device instance for the last device action matching the provided
    ///   name.
    /// </summary>
    /// <param name="visaDeviceAccessor">
    ///   The object to be searched.
    /// </param>
    /// <param name="name">
    ///   The device action name to search for. The name is not case-sensitive.
    /// </param>
    /// <returns>
    ///   The last found device action matching the provided name, or <c>null</c> otherwise.
    /// </returns>
    public static IDeviceAction? FindDeviceAction(this IVisaDeviceAccessor visaDeviceAccessor, string name) =>
      visaDeviceAccessor.FindDeviceActions(name).LastOrDefault();

    /// <summary>
    ///   Searches the object with the attached VISA device instance for the last device action matching the provided
    ///   name, and executes it.
    /// </summary>
    /// <param name="visaDeviceAccessor">
    ///   The object to be searched.
    /// </param>
    /// <param name="name">
    ///   The device action name to search for. The name is not case-sensitive.
    /// </param>
    /// <returns>
    ///   <c>true</c> if the last found matching device action has started its execution, otherwise <c>false</c>.
    ///   <c>false</c> is also returned if the matching device action cannot be executed at the moment (its
    ///   <see cref="IDeviceAction.CanExecute" /> property returns <c>false</c>).
    /// </returns>
    /// <remarks>
    ///   This method does not throw any exceptions. All exceptions caught during the device action execution can be
    ///   addressed through subscribing to the <see cref="IDeviceAction.Exception" /> event of the corresponding device
    ///   action instance.
    /// </remarks>
    public static async Task<bool> ExecuteDeviceActionAsync(this IVisaDeviceAccessor visaDeviceAccessor, string name)
    {
      if (visaDeviceAccessor.FindDeviceAction(name) is not { CanExecute: true } deviceAction)
        return false;
      await deviceAction.ExecuteAsync();
      return true;
    }
  }
}
