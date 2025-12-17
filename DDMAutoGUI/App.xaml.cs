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
    /// 


    public class AdvancedOptions
    {
        public ConnectionOptions connectionOptions { get; set; } = new ConnectionOptions();
        public DispenseOptions dispenseOptions { get; set; } = new DispenseOptions();
        public AdvancedOptions() { }
    }

    public class ConnectionOptions
    {
        public bool controller { get; set; } = true;
        public bool ioLinkDevices { get; set; } = true;
        public bool topCamera { get; set; } = true;
        public bool sideCamera { get; set; } = true;
        public bool daqDevice { get; set; } = true;
        public bool laserSensor { get; set; } = true;
        public ConnectionOptions() { }
    }

    public class DispenseOptions
    {
        public bool healthCheck { get; set; } = true;
        public bool topPhoto { get; set; } = true;
        public bool sidePhoto { get; set; } = true;
        public bool ringHeight { get; set; } = true;
        public bool dispense { get; set; } = true;
        public bool autocalibrate { get; set; } = true;
        public bool magnetPolarity { get; set; } = true;
        public DispenseOptions() { }
    }

    public partial class App : Application
    {

        public static bool GUI_SIM_MODE = false;

        public static AdvancedOptions advancedOptions = new AdvancedOptions();

        public static DeviceConnectionManager ConnectionManager { get; private set; }
        public static ControllerManager ControllerManager { get; private set; }
        public static SettingsManager SettingsManager { get; private set; }
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
            App.ConnectionManager = new DeviceConnectionManager();

            App.ControllerManager = new ControllerManager();
            App.CameraManager = new CameraManager();
            App.DAQManager = new DAQManager();

            App.LocalDataManager = new LocalDataManager();
            App.SettingsManager = new SettingsManager();

            App.ResultsManager = new ResultsManager();
            App.ReleaseInfoManager = new ReleaseInfoManager();

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
