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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DDMAutoGUI.CustomWindows
{
    /// <summary>
    /// Interaction logic for CalibPositionPanel.xaml
    /// </summary>
    public partial class CalibPositionPanel : UserControl
    {
        public CalibPositionPanel()
        {
            InitializeComponent();
        }

        public void SetupPanel()
        {

        }

        private async void CalPosBtn_Click(object sender, RoutedEventArgs e)
        {
            CalPosPrg.Visibility = Visibility.Visible;
            try
            {
                await App.ControllerManager.CalibratePosition();
                await App.ControllerManager.Home();
            }
            catch (Exception ex)
            {
                Debug.Print("Error during position calibration: " + ex.Message);
            }
            CalPosPrg.Visibility = Visibility.Collapsed;
        }
    }
}
