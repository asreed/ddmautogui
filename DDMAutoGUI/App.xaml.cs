using DDMAutoGUI.utilities;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace DDMAutoGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public static UIManager UIManager { get; private set; }
        public static SettingsManager SettingsManager { get; private set; }
        public static ControllerManager ControllerManager { get; private set; }
        public static CameraManager CameraManager { get; private set; }




        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Debug.Print("App starting up");

            App.UIManager = new UIManager();
            App.SettingsManager = new SettingsManager();
            App.ControllerManager = new ControllerManager();
            App.CameraManager = new CameraManager();







        }

        protected override void OnExit(ExitEventArgs e)
        {





            Debug.Print("App exiting");
            base.OnExit(e);
        }
    }

}
