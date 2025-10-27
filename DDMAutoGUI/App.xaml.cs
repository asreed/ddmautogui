using DDMAutoGUI.utilities;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Text.Json;
using DDMAutoGUI.Utilities;



namespace DDMAutoGUI
{
    /// <summary>
    /// Contains application-level logic and manages core components such as UI, settings, controllers, cameras, release information, and process results. Manages startup and shutdown procedures.
    /// </summary>


    public partial class App : Application
    {

        public static SettingsManager SettingsManager { get; private set; }
        public static ControllerManager ControllerManager { get; private set; }
        public static CameraManager CameraManager { get; private set; }
        public static ReleaseInfoManager ReleaseInfoManager { get; private set; }
        public static ResultsManager ResultsManager { get; private set; }
        public static LocalDataManager LocalDataManager { get; private set; }
        public static DAQManager DAQManager { get; private set; }

        public static string advancedSettingsPassword = "ddm";


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Debug.Print("App starting up");

            // order is important
            App.ControllerManager = new ControllerManager();
            App.SettingsManager = new SettingsManager();
            App.LocalDataManager = new LocalDataManager();
            App.ReleaseInfoManager = new ReleaseInfoManager();
            App.CameraManager = new CameraManager();
            App.ResultsManager = new ResultsManager();
            App.DAQManager = new DAQManager();


            //HeightCalibration.MathNetTest();
            Debug.Print("");
        }

        protected override void OnExit(ExitEventArgs e)
        {



            var data = App.LocalDataManager.localData;
            App.LocalDataManager.SaveLocalDataToFile();

            Debug.Print("App exiting");
            base.OnExit(e);
        }

    }

}
