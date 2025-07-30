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
            UIManager.Instance.UIStateChanged += connectionWindow_OnUpdateUIState;
            ControllerManager.Instance.StatusLogUpdated += connectionWindow_OnChangeStatusLog;
            ControllerManager.Instance.RobotLogUpdated += connectionWindow_OnChangeRobotLog;

            connectionWindow_OnUpdateUIState(this, EventArgs.Empty);
            connectionWindow_OnChangeStatusLog(this, EventArgs.Empty);
            connectionWindow_OnChangeRobotLog(this, EventArgs.Empty);

            loadTCSBtn.Content += ControllerManager.Instance.GetCorrectTCSVersion();
            UpdateButtonLocks();
        }

        private async void connectBtn_ClickAsync(object sender, RoutedEventArgs e)
        {
            connectBtn.IsEnabled = false;
            //await ControllerManager.Instance.ConnectAsync(ipTextBox.Text);
            //if (ControllerManager.Instance.GetUIState().isConnected)
            //{
            //    versionLabel.Content = await ControllerManager.Instance.GetTCSVersion();
            //}
            UpdateButtonLocks();
        }

        private async void disconnectBtn_ClickAsync(object sender, RoutedEventArgs e)
        {
            disconnectBtn.IsEnabled = false;
            statusSendBtn.IsEnabled = false;
            //await ControllerManager.Instance.DisconnectAsync();
            versionLabel.Content = "(no version info)";
            UpdateButtonLocks();

        }



        private async void loadTCSBtn_Click(object sender, RoutedEventArgs e)
        {
            loadTCSBtn.IsEnabled = false;
            loadTCSLabel.Content = "Attempting...";
            string response = await ControllerManager.Instance.AttemptLoadTCS(ipTextBox.Text);
            loadTCSLabel.Content = response;
            loadTCSBtn.IsEnabled = true;
        }

        private async void statusSendBtn_ClickAsync(object sender, RoutedEventArgs e)
        {
            statusSendBtn.IsEnabled = false;
            //string response = await ControllerManager.Instance.SendStatusCommandAsync(statusMessageTextBox.Text);
            UpdateButtonLocks();
        }
        private async void robotSendBtn_ClickAsync(object sender, RoutedEventArgs e)
        {
            robotSendBtn.IsEnabled = false;
            //string response = await ControllerManager.Instance.SendRobotCommandAsync(robotMessageTextBox.Text);
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
            statusLogTextBox.Text = ControllerManager.Instance.GetStatusLog();
            statusLogTextBox.ScrollToEnd();
        }
        public void connectionWindow_OnChangeRobotLog(object sender, EventArgs e)
        {
            robotLogTextBox.Text = ControllerManager.Instance.GetRobotLog();
            robotLogTextBox.ScrollToEnd();
        }

        public void UpdateButtonLocks()
        {
            if (UIManager.Instance.UI_STATE.isConnected)
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
