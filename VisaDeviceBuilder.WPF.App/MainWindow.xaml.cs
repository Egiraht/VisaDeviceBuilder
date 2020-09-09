using System.Threading;
using System.Windows;
using Localization = VisaDeviceBuilder.WPF.App.Resources.Localization;

namespace VisaDeviceBuilder.WPF.App
{
  /// <summary>
  ///   The interaction logic class for the <i>MainWindow.xaml</i> window.
  /// </summary>
  public partial class MainWindow
  {
    /// <inheritdoc />
    public MainWindow()
    {
      InitializeComponent();
    }

    /// <summary>
    ///   The callback method for handling the <see cref="DeviceControlPanel.Exception" /> events.
    /// </summary>
    /// <param name="sender">
    ///   The event sender object.
    /// </param>
    /// <param name="args">
    ///   The exception event arguments.
    /// </param>
    private void OnException(object sender, ThreadExceptionEventArgs args) => Dispatcher.Invoke(() =>
      MessageBox.Show(this, args.Exception.Message, Localization.Title, MessageBoxButton.OK, MessageBoxImage.Error));
  }
}
