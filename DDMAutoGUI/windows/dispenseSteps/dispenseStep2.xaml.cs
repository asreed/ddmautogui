using System;
using System.Collections.Generic;
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

namespace DDMAutoGUI.dispenseSteps
{
    /// <summary>
    /// Interaction logic for dispenseStep1.xaml
    /// </summary>
    public partial class dispenseStep2 : UserControl
    {
        DispenseWindow dispenseWindow;

        public dispenseStep2()
        {
            InitializeComponent();
            // can't get reference to DispenseWindow here -- not finished loading?
        }

        private void snTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            dispenseWindow = Window.GetWindow(this) as DispenseWindow;
            if (dispenseWindow != null)
            {


            }
        }

        private async void moveBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void acqBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
