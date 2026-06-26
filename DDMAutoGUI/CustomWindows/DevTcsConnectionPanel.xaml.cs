using DDMAutoGUI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DDMAutoGUI.CustomWindows
{
    /// <summary>
    /// Interaction logic for DevTcsConnectionPanel.xaml.
    /// Hosts the developer-only raw TCS status/robot console. Log text is supplied by
    /// the inherited MainWindowViewModel via {Binding StatusLog}/{Binding RobotLog};
    /// this control owns only the send/connect actions and auto-scroll.
    /// </summary>
    public partial class DevTcsConnectionPanel : UserControl
    {
        private readonly IControllerManager _controllerManager;

        public DevTcsConnectionPanel()
        {
            InitializeComponent();

            _controllerManager = App.Services?.GetService<IControllerManager>();
        }

        private async void Adv_Con_ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            await _controllerManager.Connect(Adv_Con_IPTxt.Text);

            if (_controllerManager.CONNECTION_STATE.isConnected)
            {
                Adv_Con_ContVersionTxt.Content = await _controllerManager.GetTCSVersion();
            }
        }

        private async void Adv_Con_DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            await _controllerManager.Disconnect();
            Adv_Con_ContVersionTxt.Content = "(no version info)";
        }

        private async void Adv_Con_StatusSendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            await _controllerManager.SendStatusCommand(Adv_Con_StatusMsgTxt.Text);
        }

        private async void Adv_Con_RobotSendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            await _controllerManager.SendRobotCommand(Adv_Con_RobotMsgTxt.Text);
        }

        private void Adv_Con_StatusLogTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Adv_Con_StatusSendBtn_Click(sender, e);
            }
        }

        private void Adv_Con_RobotLogTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Adv_Con_RobotSendBtn_Click(sender, e);
            }
        }

        // Auto-scroll the log views as the bound text grows. Driven by TextChanged so it
        // works regardless of which event updates the binding, and re-scrolls when the
        // tab is reselected and the control reloads.
        private void Adv_Con_StatusLogTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            Adv_Con_StatusLogTxt.ScrollToEnd();
        }

        private void Adv_Con_RobotLogTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            Adv_Con_RobotLogTxt.ScrollToEnd();
        }
    }
}