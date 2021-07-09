using System;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   <para>
  ///     The class representing an owned asynchronous property with a value of type <typeparamref name="TValue" />
  ///     that can be accessed for any owning VISA device of type <typeparamref name="TOwner" />.
  ///     The owning VISA device is defined using the <see cref="Owner" /> property.
  ///   </para>
  ///   <para>
  ///     The <see cref="IAsyncProperty{TValue}.Getter" /> and <see cref="IAsyncProperty{TValue}.Setter" /> properties
  ///     represent the corresponding value accessors of the asynchronous property while the actual read and write
  ///     operations are performed asynchronously.
  ///   </para>
  ///   <para>
  ///     The asynchronous property can be read-only, write-only, or read-write depending on the constructor overload
  ///     used for object creation.
  ///   </para>
  /// </summary>
  /// <typeparam name="TOwner">
  ///   The type of a VISA device that can own this asynchronous property.
  ///   It must implement the <see cref="IVisaDevice" /> interface.
  /// </typeparam>
  /// <typeparam name="TValue">
  ///   The type of the value this asynchronous property can access.
  /// </typeparam>
  public class OwnedAsyncProperty<TOwner, TValue> : AsyncProperty<TValue>, IOwnedAsyncProperty<TOwner, TValue>
    where TOwner : IVisaDevice
  {
    /// <inheritdoc />
    public TOwner? Owner { get; set; }

    /// <summary>
    ///   Gets the owned getter delegate to be called for the <see cref="Owner" /> VISA device when the asynchronous
    ///   property is read.
    /// </summary>
    private Func<TOwner, TValue> OwnedGetterDelegate { get; } = _ => default!;

    /// <summary>
    ///   Gets the owned setter delegate to be called for the <see cref="Owner" /> VISA device when the asynchronous
    ///   property is written.
    /// </summary>
    private Action<TOwner, TValue> OwnedSetterDelegate { get; } = (_, _) => { };

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    ///   No owning VISA device is specified for this owned asynchronous property.
    /// </exception>
    public override Func<TValue> GetterDelegate => _getterDelegate ??= () =>
    {
      ThrowIfNoOwnerSpecified();
      return OwnedGetterDelegate.Invoke(Owner!);
    };
    private Func<TValue>? _getterDelegate;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    ///   No owning VISA device is specified for this owned asynchronous property.
    /// </exception>
    public override Action<TValue> SetterDelegate => _setterDelegate ??= value =>
    {
      ThrowIfNoOwnerSpecified();
      OwnedSetterDelegate.Invoke(Owner!, value);
    };
    private Action<TValue>? _setterDelegate;

    /// <summary>
    ///   Creates a new read-only owned asynchronous property of type <typeparamref name="TValue" />.
    /// </summary>
    /// <param name="ownedGetterDelegate">
    ///   The getter delegate to be called for the <see cref="Owner" /> VISA device when the asynchronous property is
    ///   read.
    /// </param>
    public OwnedAsyncProperty(Func<TOwner, TValue> ownedGetterDelegate) : base(() => default!) =>
      OwnedGetterDelegate = ownedGetterDelegate;

    /// <summary>
    ///   Creates a new write-only owned asynchronous property of type <typeparamref name="TValue" />.
    /// </summary>
    /// <param name="ownedSetterDelegate">
    ///   The setter delegate to be called for the <see cref="Owner" /> VISA device when the asynchronous property is
    ///   written.
    /// </param>
    public OwnedAsyncProperty(Action<TOwner, TValue> ownedSetterDelegate) : base(_ => { }) =>
      OwnedSetterDelegate = ownedSetterDelegate;

    /// <summary>
    ///   Creates a new read-write owned asynchronous property of type <typeparamref name="TValue" />.
    /// </summary>
    /// <param name="ownedGetterDelegate">
    ///   The getter delegate to be called for the <see cref="Owner" /> VISA device when the asynchronous property is
    ///   read.
    /// </param>
    /// <param name="ownedSetterDelegate">
    ///   The setter delegate to be called for the <see cref="Owner" /> VISA device when the asynchronous property is
    ///   written.
    /// </param>
    public OwnedAsyncProperty(Func<TOwner, TValue> ownedGetterDelegate, Action<TOwner, TValue> ownedSetterDelegate) :
      base(() => default!, _ => { })
    {
      OwnedGetterDelegate = ownedGetterDelegate;
      OwnedSetterDelegate = ownedSetterDelegate;
    }

    /// <summary>
    ///   Throws an <see cref="InvalidOperationException" /> if <see cref="Owner" /> is <c>null</c>.
    /// </summary>
    private void ThrowIfNoOwnerSpecified()
    {
      if (Owner == null)
        throw new InvalidOperationException(
          $"No owning VISA device is specified for the owned asynchronous property \"{Name}\".");
    }

    /// <inheritdoc />
    public override object Clone() => this switch
    {
      // Read-only owned asynchronous property:
      {CanGet: true, CanSet: false} => new OwnedAsyncProperty<TOwner, TValue>(OwnedGetterDelegate)
      {
        Owner = Owner,
        Name = Name,
        AutoUpdateGetterAfterSetterCompletes = AutoUpdateGetterAfterSetterCompletes
      },

      // Write-only owned asynchronous property:
      {CanGet: false, CanSet: true} => new OwnedAsyncProperty<TOwner, TValue>(OwnedSetterDelegate)
      {
        Owner = Owner,
        Name = Name,
        AutoUpdateGetterAfterSetterCompletes = AutoUpdateGetterAfterSetterCompletes
      },

      // Read-write owned asynchronous property:
      {CanGet: true, CanSet: true} => new OwnedAsyncProperty<TOwner, TValue>(OwnedGetterDelegate, OwnedSetterDelegate)
      {
        Owner = Owner,
        Name = Name,
        AutoUpdateGetterAfterSetterCompletes = AutoUpdateGetterAfterSetterCompletes
      },

      // Invalid owned asynchronous property:
      _ => throw new InvalidOperationException()
    };
  }
}
