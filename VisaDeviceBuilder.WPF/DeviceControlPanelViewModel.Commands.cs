using System.Windows.Input;
using VisaDeviceBuilder.WPF.Components;

namespace VisaDeviceBuilder.WPF
{
  public partial class DeviceControlPanelViewModel
  {
    private ICommand? _connectCommand;
    private ICommand? _disconnectCommand;
    private ICommand? _updateCommand;
    private ICommand? _sendMessageCommand;

    /// <summary>
    ///   The command for connecting to the device.
    /// </summary>
    public ICommand ConnectCommand => _connectCommand ??=
      new RelayCommand(_ => ConnectAsync(), _ => CanConnect);

    /// <summary>
    ///   The command for disconnecting from the device.
    /// </summary>
    public ICommand DisconnectCommand => _disconnectCommand ??=
      new RelayCommand(_ => DisconnectAsync(), _ => IsConnected);

    /// <summary>
    ///   The command for updating the asynchronous properties of the connected device.
    /// </summary>
    public ICommand UpdateCommand => _updateCommand ??=
      new RelayCommand(_ => UpdateAsyncPropertiesAsync(), _ => IsConnected);

    /// <summary>
    ///   The command for sending a message to the connected device.
    /// </summary>
    public ICommand SendMessageCommand => _sendMessageCommand ??=
      new RelayCommand(_ => SendMessageAsync(), _ => IsMessageDevice && IsConnected);
  }
}
