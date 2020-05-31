using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The class representing a remote property that can be synchronized with the remote device.
  /// </summary>
  public class RemoteProperty : IRemoteProperty
  {
    /// <summary>
    ///   The backing field for the <see cref="Setter" /> property.
    /// </summary>
    private string _setter = "";

    /// <summary>
    ///   The backing field for the <see cref="Getter" /> property.
    /// </summary>
    private string _getter = "";

    /// <summary>
    ///   The backing field for the <see cref="IsModified" /> property.
    /// </summary>
    private bool _isModified = false;

    /// <summary>
    ///   The backing field for the <see cref="IsSynchronizing" /> property.
    /// </summary>
    private bool _isSynchronizing = false;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public bool IsReadOnly { get; }

    /// <inheritdoc />
    public string Setter
    {
      get => _setter;
      set
      {
        _setter = value;
        IsModified = true;
        OnPropertyChanged(nameof(Setter));
      }
    }

    /// <inheritdoc />
    public string Getter
    {
      get => _getter;
      protected set
      {
        _getter = value;
        IsModified = false;
        OnPropertyChanged(nameof(Getter));
      }
    }

    /// <inheritdoc />
    public bool IsModified
    {
      get => _isModified;
      protected set
      {
        _isModified = value;
        OnPropertyChanged(nameof(IsModified));
      }
    }

    /// <inheritdoc />
    public bool IsSynchronizing
    {
      get => _isSynchronizing;
      protected set
      {
        _isSynchronizing = value;
        OnPropertyChanged(nameof(IsSynchronizing));
      }
    }

    /// <inheritdoc />
    public string GetterRequest { get; } = "";

    /// <inheritdoc />
    public virtual Converter<string, string> SetterRequestFormatter { get; set; } = value => value;

    /// <inheritdoc />
    public virtual Converter<string, string> GetterResponseParser { get; set; } = value => value;

    /// <summary>
    ///   Creates a new remote property.
    /// </summary>
    /// <param name="name">
    ///   The name of the remote property that distinguishes it from others.
    /// </param>
    /// <param name="isReadOnly">
    ///   Defines if the property will be read-only.
    /// </param>
    public RemoteProperty(string name, bool isReadOnly)
    {
      Name = name;
      IsReadOnly = isReadOnly;
    }

    /// <inheritdoc />
    public virtual async Task SynchronizeAsync(IMessageRequestProvider requestProvider)
    {
      if (IsSynchronizing)
        return;

      IsSynchronizing = true;

      try
      {
        if (!IsReadOnly && IsModified)
          await requestProvider.SendRequestAsync(SetterRequestFormatter(Setter));

        Getter = await requestProvider.SendRequestAsync(GetterRequest);
      }
      finally
      {
        IsSynchronizing = false;
      }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    ///   Calls the <see cref="PropertyChanged" /> event.
    /// </summary>
    /// <param name="propertyName">
    ///   The name of the changed property.
    ///   The calling member name will be used if set to <c>null</c>.
    /// </param>
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null!) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}
