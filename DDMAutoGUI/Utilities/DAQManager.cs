using DDMAutoGUI.windows;
using NationalInstruments;
using NationalInstruments.DAQmx;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace DDMAutoGUI.Utilities
{

    public class DAQMatlabResults
    {
        // Corresponds to 1.0.0 version of Matlab function
        public string version { get; set; }
        public int result { get; set; }
        public int error_code { get; set; }
        public string error_message { get; set; }
    }

    public class DAQManager
    {

        //private NationalInstruments.DAQmx.Task myTask;
        //private NationalInstruments.DAQmx.Task runningTask;
        //private AsyncCallback analogCallback;
        //private AnalogMultiChannelReader analogInReader;
        //private AnalogWaveform<double>[] data;

        //private int sampleCount = 50; // count
        //private int sampleRate = 10; // Hz

        public DAQManager()
        {
            Debug.Print("DAQ manager initialized");
        }

        public void CollectDataAndProcessML(string motorName)
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory + "MatlabExecutables\\HallFnct.exe";
            string resultsPath = AppDomain.CurrentDomain.BaseDirectory + "MatlabResults\\hall_results.json";

            string motorNameML = "";
            switch (motorName)
            {
                case "ddm_57":
                    motorNameML = "57";
                    break;
                case "ddm_95":
                    motorNameML = "95";
                    break;
                case "ddm_116":
                    motorNameML = "116";
                    break;
                case "ddm_170":
                    motorNameML = "170";
                    break;
                case "ddm_170_tall":
                    motorNameML = "170Tall";
                    break;
                default:
                    motorNameML = "?";
                    break;
            }


            Debug.Print($"Starting Matlab process for motor {motorNameML}");

            Process process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = $"{resultsPath} {motorNameML}";
            process.Start();

            process.WaitForExit();

            DAQMatlabResults result = new DAQMatlabResults();
            Debug.Print($"Reading Matlab results file from: {resultsPath}");
            try
            {
                if (File.Exists(resultsPath))
                {
                    string rawJson = File.ReadAllText(resultsPath);
                    result = JsonSerializer.Deserialize<DAQMatlabResults>(rawJson);
                }
                else
                {
                    Debug.Print("Matlab results file does not exist!");
                }
            }
            catch (JsonException ex)
            {
                Debug.Print($"Error deserializing Matlab results file: {ex.Message}");
            }

            if (result != null)
            {

                Debug.Print($"Results:");
                Debug.Print($"  Version: {result.version}");
                Debug.Print($"  Result: {result.result}");
                Debug.Print($"  Error Code: {result.error_code}");
                Debug.Print($"  Error Message: {result.error_message}");
            } else
            {
                Debug.Print("Results structure null");
            }



        }





















        //public double GetVoltage()
        //{
        //    NationalInstruments.DAQmx.Task analogInTask = new NationalInstruments.DAQmx.Task();
        //    AIChannel myAIChannel;
        //    myAIChannel = analogInTask.AIChannels.CreateVoltageChannel(
        //        "dev1/ai0",
        //        "myAIChannel",
        //        AITerminalConfiguration.Rse,
        //        -10,
        //        10,
        //        AIVoltageUnits.Volts
        //    );
        //    AnalogSingleChannelReader reader = new AnalogSingleChannelReader(analogInTask.Stream);
        //    double v0 = reader.ReadSingleSample();

        //    Debug.Print($"Voltage on dev1/ai0: {v0} V");
        //    return v0;
        //}

        //public void GetVoltageTimed()
        //{
        //    if (runningTask == null) {
        //        try
        //        {
        //            Debug.Print("Starting timed voltage acquisition");

        //            // Create a new task
        //            NationalInstruments.DAQmx.Task myTask = new NationalInstruments.DAQmx.Task();

        //            // Create a virtual channel
        //            myTask.AIChannels.CreateVoltageChannel(
        //                "dev1/ai0",
        //                "",
        //                AITerminalConfiguration.Rse,
        //                -10,
        //                10,
        //                AIVoltageUnits.Volts);

        //            // Configure the timing parameters
        //            myTask.Timing.ConfigureSampleClock(
        //                "", 
        //                sampleRate,
        //                SampleClockActiveEdge.Rising, 
        //                SampleQuantityMode.ContinuousSamples, 
        //                sampleCount);

        //            // Verify the Task
        //            myTask.Control(TaskAction.Verify);

        //            //// Prepare the table for Data
        //            //InitializeDataTable(myTask.AIChannels, ref dataTable);
        //            //acquisitionDataGrid.DataSource = dataTable;

        //            runningTask = myTask;
        //            analogInReader = new AnalogMultiChannelReader(myTask.Stream);
        //            analogCallback = new AsyncCallback(AnalogInCallback);

        //            // Use SynchronizeCallbacks to specify that the object 
        //            // marshals callbacks across threads appropriately.
        //            analogInReader.SynchronizeCallbacks = true;
        //            analogInReader.BeginReadWaveform(
        //                sampleCount,
        //                analogCallback, 
        //                myTask);
        //        }
        //        catch (DaqException exception)
        //        {
        //            // Display Errors
        //            MessageBox.Show(exception.Message);
        //            runningTask = null;
        //            myTask.Dispose();

        //        }
        //    }
        //}
        //private void AnalogInCallback(IAsyncResult ar)
        //{
        //    try
        //    {
        //        if (runningTask != null && runningTask == ar.AsyncState)
        //        {
        //            Debug.Print("AnalogInCallback called");

        //            // Read the available data from the channels
        //            data = analogInReader.EndReadWaveform(ar);

        //            // Plot your data here
        //            //dataToDataTable(data, ref dataTable);

        //            analogInReader.BeginMemoryOptimizedReadWaveform(
        //                sampleCount,
        //                analogCallback, 
        //                myTask, 
        //                data);

        //            int len = data[0].SampleCount;
        //            for (int i = 0; i < len; i++)
        //            {
        //                Debug.Print($"{data[0].Samples[i].Value}");
        //            }
        //        }
        //    }
        //    catch (DaqException exception)
        //    {
        //        // Display Errors
        //        MessageBox.Show(exception.Message);
        //        runningTask = null;
        //        myTask.Dispose();
        //    }
        //}

    }
}
