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

    public static class ErrorCodes
    {
        public static ErrorCode conCont     = new ErrorCode(-5000, "Unable to connect to controller");
        public static ErrorCode conSettings = new ErrorCode(-5001, "Unable to load settings from controller");
        public static ErrorCode conIOMaster = new ErrorCode(-5000, "Unable to connect to I/O-Link Master");
        public static ErrorCode conIO1      = new ErrorCode(-5000, "Unable to connect to I/O-Link device 1 (flow sensor 1)");
        public static ErrorCode conIO2      = new ErrorCode(-5000, "Unable to connect to I/O-Link device 2 (flow sensor 2)");
        public static ErrorCode conIO3      = new ErrorCode(-5000, "Unable to connect to I/O-Link device 3 (analog input)");
        public static ErrorCode conIO4      = new ErrorCode(-5000, "Unable to connect to I/O-Link device 4 (analog output)");
        public static ErrorCode conLaser    = new ErrorCode(-5000, "Unable to connect to laser displacement sensor");
        public static ErrorCode conCamTop   = new ErrorCode(-5000, "Unable to connect to connect to top camera");
        public static ErrorCode conCamSide  = new ErrorCode(-5000, "Unable to connect to connect to side camera");
    }

    public class ErrorCode
    {
        public int code { get; set; } = -1;
        public string msg { get; set; } = "";
        public ErrorCode(int _code, string _msg)
        {
            code = _code;
            msg = _msg;
        }
    }

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
        public bool checkHealth { get; set; } = true;
        public bool photoTop { get; set; } = true;
        public bool photoSide { get; set; } = true;
        public bool measureHeights { get; set; } = true;
        public bool dispense { get; set; } = true;
        public bool autocalibrate { get; set; } = true;
        public bool checkPolarity { get; set; } = true;
        public bool photoTopAfter { get; set; } = true;
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
        public static OCRManager OCRManager { get; private set; }

        public static string calibrationPassword = "ddm";
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
            App.OCRManager = new OCRManager();

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
