using System.Windows.Controls;

namespace DDMAutoGUI.CustomWindows
{
    public partial class ConnectPanel : UserControl
    {
        public ConnectPanel()
        {
            InitializeComponent();
        }

        // Auto-scroll the connection log without imperative wiring from MainWindow.
        private void LogTxt_TextChanged(object sender, TextChangedEventArgs e)
            => LogTxt.ScrollToEnd();
    }
}