// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Threading;
using System.Threading.Tasks;

namespace VisaDeviceBuilder.Abstracts
{
  /// <summary>
  ///   The interface for asynchronous properties.
  /// </summary>
  public interface IAsyncProperty : ICloneable
  {
    /// <summary>
    ///   Gets or sets the optional user-readable name of the asynchronous property.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    ///   The VISA device instance this asynchronous property currently targets.
    ///   This reference will be passed to the property's getter and setter delegates during read and write operations
    ///   respectively, so ensure it is set correctly before any read or write accesses.
    ///   May be set to <c>null</c>, if the asynchronous property does not require a VISA device instance for
    ///   functioning.
    /// </summary>
    IVisaDevice? TargetDevice { get; set; }

    /// <summary>
    ///   Checks if the asynchronous property can be read.
    /// </summary>
    bool CanGet { get; }

    /// <summary>
    ///   Checks if the asynchronous property can be written.
    /// </summary>
    bool CanSet { get; }

    /// <summary>
    ///   Gets the actual value <see cref="Type" /> of this asynchronous property.
    /// </summary>
    Type ValueType { get; }

    /// <summary>
    ///   Gets the cached value of the asynchronous property acquired from the last getter update.
    ///   Exceptions thrown during the new value processing can be handled using the <see cref="GetterException" />
    ///   event.
    /// </summary>
    object? Getter { get; }

    /// <summary>
    ///   Sets the new value of the asynchronous property.
    ///   Exceptions thrown during the new value processing can be handled using the <see cref="SetterException" />
    ///   event.
    /// </summary>
    object? Setter { set; }

    /// <summary>
    ///   Gets of sets the flag indicating if the <see cref="IAsyncProperty.Getter" /> property value should be automatically updated
    ///   after new <see cref="IAsyncProperty.Setter" /> property value processing completes.
    ///   Setting this value to <c>true</c> can be useful if no supplementary <see cref="IAutoUpdater" /> is used
    ///   to periodically update the getter.
    /// </summary>
    bool AutoUpdateGetterAfterSetterCompletes { get; set; }

    /// <summary>
    ///   The event called when the new getter value is updated.
    /// </summary>
    event EventHandler? GetterUpdated;

    /// <summary>
    ///   The event called when the setter value processing is completed.
    /// </summary>
    event EventHandler? SetterCompleted;

    /// <summary>
    ///   The event called on getter failure.
    /// </summary>
    event ThreadExceptionEventHandler? GetterException;

    /// <summary>
    ///   The event called on setter failure.
    /// </summary>
    event ThreadExceptionEventHandler? SetterException;

    /// <summary>
    ///   Requests the asynchronous update of the <see cref="IAsyncProperty.Getter" /> property.
    ///   Exceptions thrown during the update can be handled using the <see cref="GetterException" /> event while
    ///   this method does not throw any exceptions.
    /// </summary>
    void RequestGetterUpdate();

    /// <summary>
    ///   Gets the <see cref="Task" /> object wrapping the asynchronous <see cref="IAsyncProperty.Getter" /> value updating.
    ///   This object can be awaited until the value updating is finished.
    /// </summary>
    /// <returns>
    ///   The running <see cref="IAsyncProperty.Getter" /> updating <see cref="Task" /> object or the
    ///   <see cref="Task.CompletedTask" /> object if no <see cref="IAsyncProperty.Getter" /> updating is running at the moment.
    /// </returns>
    Task GetGetterUpdatingTask();

    /// <summary>
    ///   Gets the <see cref="Task" /> object wrapping the asynchronous new <see cref="IAsyncProperty.Setter" /> value processing.
    ///   This object can be awaited until the value processing is finished.
    /// </summary>
    /// <returns>
    ///   The running <see cref="IAsyncProperty.Setter" /> processing <see cref="Task" /> object or the
    ///   <see cref="Task.CompletedTask" /> object if no <see cref="IAsyncProperty.Setter" /> processing is running at the moment.
    /// </returns>
    Task GetSetterProcessingTask();
  }
}
