using System;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using VisaDeviceBuilder.Abstracts;
using LocalizationResourceManager = System.Resources.ResourceManager;

namespace VisaDeviceBuilder.WPF
{
  /// <summary>
  ///   The interaction logic class for the <i>DeviceControlPanel.xaml</i> user control.
  /// </summary>
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
    ///   Gets or sets the text label used for device distinguishing among the devices of similar type.
    /// </summary>
    public string DeviceLabel
    {
      get => ViewModel.DeviceLabel;
      set => ViewModel.DeviceLabel = value;
    }

    /// <summary>
    ///   Checks or sets if the auto-updater for asynchronous properties is enabled.
    /// </summary>
    public bool IsAutoUpdaterEnabled
    {
      get => ViewModel.IsAutoUpdaterEnabled;
      set => ViewModel.IsAutoUpdaterEnabled = value;
    }

    /// <summary>
    ///   Gets or sets the auto-updater cycle delay in milliseconds.
    /// </summary>
    public int AutoUpdaterDelay
    {
      get => ViewModel.AutoUpdaterDelay;
      set => ViewModel.AutoUpdaterDelay = value;
    }

    /// <summary>
    ///   Gets or sets the flag indicating if the custom message input panel should be enabled.
    /// </summary>
    public bool IsMessageInputPanelEnabled
    {
      get => ViewModel.IsMessageInputPanelEnabled;
      set => ViewModel.IsMessageInputPanelEnabled = value;
    }

    /// <summary>
    ///   Gets or sets the optional ResX resource manager instance used for localization of the names of available
    ///   asynchronous properties and actions.
    ///   The provided localization resource manager must be able to accept the original names of the asynchronous
    ///   properties and actions and return their localized names.
    ///   If not provided, the original names will be used without localization.
    /// </summary>
    public LocalizationResourceManager? LocalizationResourceManager
    {
      get => ViewModel.LocalizationResourceManager;
      set => ViewModel.LocalizationResourceManager = value;
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
    public DeviceControlPanel(IVisaDevice device)
    {
      InitializeComponent();

      DataContext = new DeviceControlPanelViewModel(device);
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
