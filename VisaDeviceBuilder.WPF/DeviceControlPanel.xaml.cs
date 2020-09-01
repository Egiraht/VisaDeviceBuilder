using System;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using Ivi.Visa;

namespace VisaDeviceBuilder.WPF
{
  /// <summary>
  ///   The interaction logic class for the <i>DeviceControlPanel.xaml</i> user control.
  /// </summary>
  // TODO: Convert the public properties into dependency properties.
  public partial class DeviceControlPanel
  {
    /// <summary>
    ///   Gets the view model instance of the current control.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///   The control's <see cref="Control.DataContext"/> is not a <see cref="DeviceControlPanelViewModel" /> class.
    /// </exception>
    public DeviceControlPanelViewModel ViewModel => DataContext is DeviceControlPanelViewModel viewModel
      ? viewModel
      : throw new InvalidOperationException(
        $"The control's {nameof(DataContext)} is not a {nameof(DeviceControlPanelViewModel)} class.");

    /// <summary>
    ///   Gets or sets the type of the device.
    ///   The device class defined by the specified type must implement the <see cref="IVisaDevice" /> interface.
    /// </summary>
    /// <exception cref="InvalidCastException">
    ///   The provided type value does not implement the <see cref="IVisaDevice" /> interface.
    /// </exception>
    public Type DeviceType
    {
      get => ViewModel.DeviceType;
      set => ViewModel.DeviceType = value;
    }

    /// <summary>
    ///   Gets or sets the type of the VISA resource manager.
    ///   The resource manager class defined by the specified type must implement the <see cref="IResourceManager" />
    ///   interface, or the value can be <c>null</c>.
    ///   If set to <c>null</c>, the default <see cref="GlobalResourceManager" /> static class will be used.
    /// </summary>
    /// <exception cref="InvalidCastException">
    ///   The provided type value does not implement the <see cref="IResourceManager" /> interface.
    /// </exception>
    public Type? ResourceManagerType
    {
      get => ViewModel.ResourceManagerType;
      set => ViewModel.ResourceManagerType = value;
    }

    /// <summary>
    ///   Gets or sets the text label used for device distinguishing among the devices of similar type.
    /// </summary>
    public string DeviceLabel
    {
      get => ViewModel.DeviceLabel;
      set => ViewModel.DeviceLabel = value;
    }

    /// <summary>
    ///   Checks or sets the value if the command input should be enabled.
    /// </summary>
    public bool IsCommandInputEnabled
    {
      get => ViewModel.IsCommandInputEnabled;
      set => ViewModel.IsCommandInputEnabled = value;
    }

    /// <summary>
    ///   Gets or sets the resource name used for VISA device connection.
    /// </summary>
    public string ResourceName
    {
      get => ViewModel.ResourceName;
      set => ViewModel.ResourceName = value;
    }

    /// <summary>
    ///   Event that is called on any control exception.
    /// </summary>
    public event ThreadExceptionEventHandler? Exception
    {
      add => ViewModel.Exception += value;
      remove => ViewModel.Exception -= value;
    }

    /// <inheritdoc />
    public DeviceControlPanel()
    {
      InitializeComponent();
    }

    /// <summary>
    ///   The callback for the <see cref="ResourceNamesComboBox" /> drop-down event.
    /// </summary>
    /// <param name="sender">
    ///   The event sender object.
    /// </param>
    /// <param name="e">
    ///   The event arguments object.
    /// </param>
    private async void OnResourceNamesComboBoxDropDownOpened(object sender, EventArgs e) =>
      await ViewModel.UpdateResourcesListAsync();

    /// <summary>
    ///   Updates the data source value bound to the sender <see cref="TextBox" /> control on <i>Enter</i> key-down
    ///   event.
    /// </summary>
    /// <param name="sender">
    ///   The event sender object which bindings need to be updated.
    /// </param>
    /// <param name="e">
    ///   The event arguments object.
    /// </param>
    private void UpdateTextBoxValueOnEnterKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key != Key.Enter || !(sender is TextBox textBox))
        return;

      try
      {
        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
      }
      catch
      {
        // Ignore exceptions.
      }
    }
  }
}
