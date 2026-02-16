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
using System.Windows.Shapes;

namespace DDMAutoGUI.windows
{
    /// <summary>
    /// Interaction logic for TextDataViewer.xaml
    /// </summary>
    public partial class TextDataViewer : Window
    {
        public bool confirm = false;

        public TextDataViewer()
        {
            InitializeComponent();
        }

        public void PopulateData(string data)
        {
            dataTextBlock.Text = data;
        }
        public void PopulateData(string data, string dataName)
        {
            dataNameLabel.Content = dataName;
            dataTextBlock.Text = data;
        }

        private void okBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
