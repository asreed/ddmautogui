using System.Windows.Controls;

namespace DDMAutoGUI.CustomWindows
{
    /// <summary>
    /// Interaction logic for CalibPanel.xaml.
    /// </summary>
    public partial class CalibPanel : UserControl
    {
        public CalibPanel()
        {
            InitializeComponent();
        }

        public void SetupPanel()
        {
            CalPosPanel.SetupPanel();
            //CalFlowPanel.SetupPanel();
        }
    }
}
