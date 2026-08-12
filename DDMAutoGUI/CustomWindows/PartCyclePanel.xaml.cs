using DDMAutoGUI.Services;
using Microsoft.Extensions.DependencyInjection;
using DDMAutoGUI.ViewModels;
using DDMAutoGUI.windows;
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

namespace DDMAutoGUI.CustomWindows
{
    /// <summary>
    /// Interaction logic for PartCyclePanel.xaml
    /// </summary>
    public partial class PartCyclePanel : UserControl
    {
        public PartCyclePanel()
        {
            InitializeComponent();
        }

        private void LogTxt_TextChanged(object sender, TextChangedEventArgs e)
        => LogTxt.ScrollToEnd();

        private void MotorSizeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && DataContext is MainWindowViewModel vm)
            {
                vm.SelectedMotorType = rb.Tag as string;
            }
        }
        private void Disp_Res_FinishBtn_Click(object sender, RoutedEventArgs e)
        {
            dispTabControl.SelectedIndex = 0;
        }

        private void Disp_Res_ViewResBtn_Click(object sender, RoutedEventArgs e)
        {
            var resultsManager = App.Services?.GetService<IResultsService>();
            if (resultsManager == null) return;

            string data_string = resultsManager.GetCurrentResultsAsString();
            TextDataViewer viewer = new TextDataViewer();

            if (data_string != null)
            {
                viewer.Owner = Window.GetWindow(this);
                viewer.PopulateData(data_string, "Results Data");
                viewer.ShowDialog();
            }
        }
        private void Disp_OpenFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            var resultsManager = App.Services?.GetService<IResultsService>();
            if (resultsManager != null)
            {
                resultsManager.OpenBrowserToDirectory();
            }
        }

        private void Disp_Res_OpenFileBtn_Click(object sender, RoutedEventArgs e)
        {
            var resultsManager = App.Services?.GetService<IResultsService>();
            if (resultsManager != null)
            {
                resultsManager.OpenBrowserToDirectory();
            }
        }
    }
}
