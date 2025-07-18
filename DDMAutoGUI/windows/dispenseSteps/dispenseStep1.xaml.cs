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
    public partial class dispenseStep1 : UserControl
    {
        private DispenseWindow dispenseWindow;

        public dispenseStep1()
        {
            InitializeComponent();
        }
        public dispenseStep1(DispenseWindow dispenseWindow)
        {
            this.dispenseWindow = dispenseWindow;
            InitializeComponent();
        }

        public string GetSN()
        {
            return snTextBox.Text;
        }

        private void snTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dispenseWindow != null)
            {
                if (snTextBox.Text.Length > 0)
                {

                }
                else
                {

                }
            }
        }
    }
}
