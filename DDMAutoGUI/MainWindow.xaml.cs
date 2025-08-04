using DDMAutoGUI.utilities;
using DDMAutoGUI.windows;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DDMAutoGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private static ConnectionWindow connectionWindow;
        private static ControlPanelWindow debugWindow;
        private static DispenseWindow dispenseWindow;
        private static DispenseWindow2 dispenseWindow2;
        private static CameraWindow cameraWindow;
        private static SettingsWindow settingsWindow;

        public MainWindow()
        {

            // snippet to attempt to fix the strange menu alignment
            // https://www.red-gate.com/simple-talk/blogs/wpf-menu-displays-to-the-left-of-the-window/

            var menuDropAlignmentField = typeof(SystemParameters).GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
            Action setAlignmentValue = () => {
                if (SystemParameters.MenuDropAlignment && menuDropAlignmentField != null) menuDropAlignmentField.SetValue(null, false);
            };
            setAlignmentValue();
            SystemParameters.StaticPropertyChanged += (sender, e) => { setAlignmentValue(); };

            //



            InitializeComponent();
            App.UIManager.UIStateChanged += mainWindow_OnChangeState;

            this.Title += App.UIManager.GetAppVersionString();

            splashErrorBox.Visibility = Visibility.Collapsed;
            UpdateButtonLocks();
        }



        private async void splashConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            splashConnectBtn.IsEnabled = false;
            splashConnectBtn.Content = "Connecting...";
            splashErrorBox.Visibility = Visibility.Collapsed;

            await App.ControllerManager.ConnectAsync(splashIPTextBox.Text);
            if (App.UIManager.UI_STATE.isConnected)
            {
                splashErrorBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                splashErrorBox.Visibility = Visibility.Visible;
                splashErrorLabel.Text = "Connection failed. Verify IP is correct and TCS is running.";
                splashConnectBtn.IsEnabled = true;
            }
        }
        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (connectionWindow == null || !connectionWindow.IsVisible)
            {
                connectionWindow = new ConnectionWindow();
                connectionWindow.Closed += (s, e) => connectionWindow = null;
                //connectionWindow.Owner = this;
                connectionWindow.Show();
            }
            else
            {
                connectionWindow.Activate();
            }
        }

        private void DispenseBtn_Click(object sender, RoutedEventArgs e)
        {
            dispenseWindow2 = new DispenseWindow2();
            //dispenseWindow2.Owner = this;
            dispenseWindow2.Show();
        }

        private void DebugBtn_Click(object sender, RoutedEventArgs e)
        {
            if (debugWindow == null || !debugWindow.IsVisible)
            {
                debugWindow = new ControlPanelWindow();
                debugWindow.Closed += (s, e) => debugWindow = null;
                //debugWindow.Owner = this;
                debugWindow.Show();

            }
            else
            {
                debugWindow.Activate();
            }
        }

        private void CameraBtn_Click(object sender, RoutedEventArgs e)
        {
            if (cameraWindow == null || !cameraWindow.IsVisible)
            {
                cameraWindow = new CameraWindow();
                cameraWindow.Closed += (s, e) => cameraWindow = null;
                //cameraWindow.Owner = this;
                cameraWindow.Show();
            }
            else
            {
                cameraWindow.Activate();
            }
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (settingsWindow == null || !settingsWindow.IsVisible)
            {
                settingsWindow = new SettingsWindow();
                settingsWindow.Closed += (s, e) => settingsWindow = null;
                //settingsWindow.Owner = this;
                settingsWindow.Show();
            }
            else
            {
                settingsWindow.Activate();
            }
        }

        public void mainWindow_OnChangeState(object sender, EventArgs e)
        {
            UpdateButtonLocks();
        }

        private void UpdateButtonLocks()
        {
            if (App.UIManager.UI_STATE.isConnected)
            {
                splashConnectBtn.Content = "Connected";
                splashErrorBox.Visibility = Visibility.Collapsed;
                splashConnectBtn.IsEnabled = false;
                //debugBtn.IsEnabled = true;
                //debugMenuItem.IsEnabled = true;
                //debugToolbarBtn.IsEnabled = true;

                //if (state.isDispenseWizardActive)
                //{
                //    dispenseBtn.IsEnabled = false;
                //}
                //else
                //{
                //    dispenseBtn.IsEnabled = true;
                //}
            }
            else
            {
                splashConnectBtn.Content = "Connect";
                splashConnectBtn.IsEnabled = true;
                //dispenseBtn.IsEnabled = false;
                //debugBtn.IsEnabled = false;
                //debugMenuItem.IsEnabled = false;
                //debugToolbarBtn.IsEnabled = false;

            }

        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string formattedVersion = $"{v.Major}.{v.Minor}.{v.Build}";

            string versionInfo = "DDM Automation Cell\n";
            versionInfo += "UI Version: " + formattedVersion + "\n";
            versionInfo += "Intended for engineering use only. Not intended for production.\n";

            //MessageBox.Show(this, versionInfo, "About", MessageBoxButton.OK, MessageBoxImage.Information);

            InfoWindow infoWindow = new InfoWindow();
            infoWindow.Owner = this;
            infoWindow.ShowDialog();

        }
    }
}