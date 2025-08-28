using DDMAutoGUI.utilities;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Text.Json;

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

            //DDMResults results = TestBuildResults();

            //var options = new JsonSerializerOptions { WriteIndented = true };
            //string resultsString = JsonSerializer.Serialize<DDMResults>(results, options);
            //Debug.Print("Test results JSON:\n" + resultsString);


        }

        private DDMResults TestBuildResults()
        {

            DDMResults results = new DDMResults();
            results.date_saved = DateTime.Now;
            results.ring_sn = "test ring sn 33333";
            DDMResultsOptions options = new DDMResultsOptions();
            options.pre_photo = true;
            options.ring_measure = true;
            options.mag_measure = true;
            options.shot_id = true;
            options.shot_od = true;
            options.post_photo = true;
            results.selected_options = options;
            options.post_photo = false;
            results.completed_options = options;
            //DDMResultsShot shot_id = new DDMResultsShot();
            //shot_id.valve_num = 1;
            //shot_id.vol = 0.38f;
            //shot_id.time = 5f;
            //shot_id.pressure = 48f;
            //results.shot_id = shot_id;
            //DDMResultsShot shot_od = new DDMResultsShot();
            //shot_od.valve_num = 2;
            //shot_od.vol = 0.46f;
            //shot_od.time = 5f;
            //shot_od.pressure = 15f;
            //results.shot_od = shot_od;
            List<DDMResultsSingleHeight> ring_heights = new List<DDMResultsSingleHeight>(6);
            for (int i = 0; i < ring_heights.Capacity; i++)
            {
                Random r = new Random();
                float ti = 360f / ring_heights.Capacity * i;
                float yi = 0.100f + 0.05f * (float)r.NextDouble();
                ring_heights.Insert(i, new DDMResultsSingleHeight() { t = ti, z = yi });
            }
            results.ring_heights = ring_heights;
            List<DDMResultsSingleHeight> mag_heights = new List<DDMResultsSingleHeight>(50);
            for (int i = 0; i < mag_heights.Capacity; i++)
            {
                Random r = new Random();
                float ti = 360f / mag_heights.Capacity * i;
                float yi = 0.200f + 0.05f * (float)r.NextDouble();
                mag_heights.Insert(i, new DDMResultsSingleHeight() { t = ti, z = yi });
            }
            results.mag_heights = mag_heights;
            List<DDMResultsLogLine> process_log = new List<DDMResultsLogLine>();
            process_log.Add(new DDMResultsLogLine() { date = DateTime.Now, message = "Test log line 1" });
            process_log.Add(new DDMResultsLogLine() { date = DateTime.Now, message = "Test log line 2" });
            process_log.Add(new DDMResultsLogLine() { date = DateTime.Now, message = "Test log line 3" });
            process_log.Add(new DDMResultsLogLine() { date = DateTime.Now, message = "Test log line 4" });
            process_log.Add(new DDMResultsLogLine() { date = DateTime.Now, message = "Test log line 5" });

            results.process_log = process_log;
            return results;

        }

        protected override void OnExit(ExitEventArgs e)
        {





            Debug.Print("App exiting");
            base.OnExit(e);
        }
    }

}
