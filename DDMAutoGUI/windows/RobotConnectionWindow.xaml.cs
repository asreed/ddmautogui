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
    public partial class RobotConnectionWindow : Window
    {


        public RobotConnectionWindow()
        {
            InitializeComponent();
            //RobotManager.Instance.SetMessageLog(statusLogTextBox);
            RobotManager.Instance.UpdateUIState += connectionWindow_OnUpdateUIState;
            RobotManager.Instance.ChangeStatusLog += connectionWindow_OnChangeStatusLog;
            RobotManager.Instance.ChangeRobotLog += connectionWindow_OnChangeRobotLog;

            connectionWindow_OnUpdateUIState(this, EventArgs.Empty);
            connectionWindow_OnChangeStatusLog(this, EventArgs.Empty);
            connectionWindow_OnChangeRobotLog(this, EventArgs.Empty);

            loadTCSBtn.Content += RobotManager.Instance.GetCorrectTCSVersion();
            UpdateButtonLocks();
        }

        private async void connectBtn_ClickAsync(object sender, RoutedEventArgs e)
        {
            connectBtn.IsEnabled = false;
            await RobotManager.Instance.ConnectAsync(ipTextBox.Text);
            if (RobotManager.Instance.GetUIState().isConnected)
            {
                versionLabel.Content = await RobotManager.Instance.GetControllerSoftwareVersionAsync();
            }
            UpdateButtonLocks();
        }

        private async void disconnectBtn_ClickAsync(object sender, RoutedEventArgs e)
        {
            disconnectBtn.IsEnabled = false;
            statusSendBtn.IsEnabled = false;
            await RobotManager.Instance.DisconnectAsync();
            versionLabel.Content = "(no version info)";
            UpdateButtonLocks();

        }



        private async void loadTCSBtn_Click(object sender, RoutedEventArgs e)
        {
            loadTCSBtn.IsEnabled = false;
            loadTCSLabel.Content = "Attempting...";
            string response = await RobotManager.Instance.AttemptLoadTCS(ipTextBox.Text);
            loadTCSLabel.Content = response;
            loadTCSBtn.IsEnabled = true;
        }

        private async void statusSendBtn_ClickAsync(object sender, RoutedEventArgs e)
        {
            statusSendBtn.IsEnabled = false;
            string response = await RobotManager.Instance.SendStatusCommandAsync(statusMessageTextBox.Text);
            UpdateButtonLocks();
        }
        private async void robotSendBtn_ClickAsync(object sender, RoutedEventArgs e)
        {
            robotSendBtn.IsEnabled = false;
            string response = await RobotManager.Instance.SendRobotCommandAsync(robotMessageTextBox.Text);
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
            statusLogTextBox.Text = RobotManager.Instance.GetStatusLog();
            statusLogTextBox.ScrollToEnd();
        }
        public void connectionWindow_OnChangeRobotLog(object sender, EventArgs e)
        {
            robotLogTextBox.Text = RobotManager.Instance.GetRobotLog();
            robotLogTextBox.ScrollToEnd();
        }

        public void UpdateButtonLocks()
        {
            UIState state = RobotManager.Instance.GetUIState();
            if (state.isConnected)
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
