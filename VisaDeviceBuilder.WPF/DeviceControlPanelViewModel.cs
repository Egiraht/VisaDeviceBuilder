using System.Windows.Input;
using VisaDeviceBuilder.WPF.Components;

namespace VisaDeviceBuilder.WPF
{
  /// <summary>
  ///   The view model class for the <see cref="DeviceControlPanel" /> WPF control. It is based on the
  ///   <see cref="VisaDeviceController" /> class and adds the WPF-specific command interaction logic.
  /// </summary>
  public class DeviceControlPanelViewModel : VisaDeviceController
  {
    /* The private backing fields. */
    private ICommand? _updateResourcesListCommand;
    private ICommand? _connectCommand;
    private ICommand? _disconnectCommand;
    private ICommand? _updateAsyncPropertiesCommand;
    private ICommand? _sendMessageCommand;

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
      base.OnPropertyChanged(propertyName);

      // Forces all RelayCommand instances to call their CanExecuteChanged events.
      // This fixes some buttons that do not get their states updated after corresponding RelayCommand execution.
      CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    ///   The command for updating the list of available VISA resources.
    /// </summary>
    public ICommand UpdateResourcesListCommand => _updateResourcesListCommand ??=
      new RelayCommand(_ => UpdateResourcesListAsync());

    /// <summary>
    ///   The command for connecting to the device.
    /// </summary>
    public ICommand ConnectCommand => _connectCommand ??=
      new RelayCommand(_ => Connect(), _ => !string.IsNullOrWhiteSpace(ResourceName) && CanConnect);

    /// <summary>
    ///   The command for disconnecting from the device.
    /// </summary>
    public ICommand DisconnectCommand => _disconnectCommand ??=
      new RelayCommand(_ => DisconnectAsync(), _ => !CanConnect && !IsDisconnectionRequested);

    /// <summary>
    ///   The command for updating the asynchronous properties of the connected device.
    /// </summary>
    public ICommand UpdateAsyncPropertiesCommand => _updateAsyncPropertiesCommand ??=
      new RelayCommand(_ => UpdateAsyncPropertiesAsync(), _ => !IsUpdatingAsyncProperties);

    /// <summary>
    ///   The command for sending a message to the connected device.
    /// </summary>
    public ICommand SendMessageCommand => _sendMessageCommand ??=
      new RelayCommand(_ => SendMessageAsync(), _ => IsMessageDevice && IsDeviceReady);
  }
}
