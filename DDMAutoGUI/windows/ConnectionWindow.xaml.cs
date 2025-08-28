using DDMAutoGUI.utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DDMAutoGUI.windows
{
    /// <summary>
    /// Interaction logic for RobotConnectionWindow.xaml
    /// </summary>
    public partial class ConnectionWindow : Window
    {


        public ConnectionWindow()
        {
            InitializeComponent();
            //RobotManager.Instance.SetMessageLog(statusLogTextBox);
            App.UIManager.UIStateChanged += connectionWindow_OnUpdateUIState;
            App.ControllerManager.StatusLogUpdated += connectionWindow_OnChangeStatusLog;
            App.ControllerManager.RobotLogUpdated += connectionWindow_OnChangeRobotLog;

            connectionWindow_OnUpdateUIState(this, EventArgs.Empty);
            connectionWindow_OnChangeStatusLog(this, EventArgs.Empty);
            connectionWindow_OnChangeRobotLog(this, EventArgs.Empty);

            loadTCSBtn.Content += App.ControllerManager.GetCorrectTCSVersion();
            UpdateButtonLocks();
        }

        private async void connectBtn_ClickAsync(object sender, RoutedEventArgs e)
        {
            connectBtn.IsEnabled = false;
            await App.ControllerManager.Connect(ipTextBox.Text);
            if (App.UIManager.UI_STATE.isConnected)
            {
                versionLabel.Content = await App.ControllerManager.GetTCSVersion();
            }
            UpdateButtonLocks();
        }

        private async void disconnectBtn_ClickAsync(object sender, RoutedEventArgs e)
        {
            disconnectBtn.IsEnabled = false;
            statusSendBtn.IsEnabled = false;
            await App.ControllerManager.Disconnect();
            versionLabel.Content = "(no version info)";
            UpdateButtonLocks();

        }

        private async void loadTCSBtn_Click(object sender, RoutedEventArgs e)
        {
            //loadTCSBtn.IsEnabled = false;
            //loadTCSLabel.Content = "Attempting...";
            //string response = await App.ControllerManager.AttemptLoadTCS(ipTextBox.Text);
            //loadTCSLabel.Content = response;
            //loadTCSBtn.IsEnabled = true;
        }

        private async void statusSendBtn_ClickAsync(object sender, RoutedEventArgs e)
        {
            statusSendBtn.IsEnabled = false;
            string response = await App.ControllerManager.SendStatusCommand(statusMessageTextBox.Text);
            UpdateButtonLocks();
        }
        private async void robotSendBtn_ClickAsync(object sender, RoutedEventArgs e)
        {
            robotSendBtn.IsEnabled = false;
            string response = await App.ControllerManager.SendRobotCommand(robotMessageTextBox.Text);
            UpdateButtonLocks();
        }


        private void statusMessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                statusSendBtn_ClickAsync(sender, e);
            }
        }

        private void robotMessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                robotSendBtn_ClickAsync(sender, e);
            }
        }

        private void startAutoBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        private void stopAutoBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        public void connectionWindow_OnUpdateUIState(object sender, EventArgs e)
        {
            UpdateButtonLocks();
        }

        public void connectionWindow_OnChangeStatusLog(object sender, EventArgs e)
        {
            statusLogTextBox.Text = App.ControllerManager.GetStatusLog();
            statusLogTextBox.ScrollToEnd();
        }
        public void connectionWindow_OnChangeRobotLog(object sender, EventArgs e)
        {
            robotLogTextBox.Text = App.ControllerManager.GetRobotLog();
            robotLogTextBox.ScrollToEnd();
        }

        public void UpdateButtonLocks()
        {
            if (App.UIManager.UI_STATE.isConnected)
            {
                connectBtn.IsEnabled = false;
                disconnectBtn.IsEnabled = true;
                statusSendBtn.IsEnabled = true;
                robotSendBtn.IsEnabled = true;
            }
            else
            {
                connectBtn.IsEnabled = true;
                disconnectBtn.IsEnabled = false;
                statusSendBtn.IsEnabled = false;
                robotSendBtn.IsEnabled = false;
            }
        }
    }
}
