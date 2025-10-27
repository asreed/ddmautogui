using NationalInstruments;
using NationalInstruments.DAQmx;
using NationalInstruments.DAQmx;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DDMAutoGUI.Utilities
{
    public class DAQManager
    {


        private NationalInstruments.DAQmx.Task myTask;
        private NationalInstruments.DAQmx.Task runningTask;
        private AsyncCallback analogCallback;
        private AnalogMultiChannelReader analogInReader;
        private AnalogWaveform<double>[] data;

        private int sampleCount = 50; // count
        private int sampleRate = 10; // Hz



        public DAQManager()
        {
            Debug.Print("DAQ manager initialized");
        }

        public double GetVoltage()
        {
            NationalInstruments.DAQmx.Task analogInTask = new NationalInstruments.DAQmx.Task();
            AIChannel myAIChannel;
            myAIChannel = analogInTask.AIChannels.CreateVoltageChannel(
                "dev1/ai0",
                "myAIChannel",
                AITerminalConfiguration.Rse,
                -10,
                10,
                AIVoltageUnits.Volts
            );
            AnalogSingleChannelReader reader = new AnalogSingleChannelReader(analogInTask.Stream);
            double v0 = reader.ReadSingleSample();

            Debug.Print($"Voltage on dev1/ai0: {v0} V");
            return v0;
        }




        public void GetVoltageTimed()
        {
            if (runningTask == null) {
                try
                {
                    Debug.Print("Starting timed voltage acquisition");

                    // Create a new task
                    NationalInstruments.DAQmx.Task myTask = new NationalInstruments.DAQmx.Task();

                    // Create a virtual channel
                    myTask.AIChannels.CreateVoltageChannel(
                        "dev1/ai0",
                        "",
                        AITerminalConfiguration.Rse,
                        -10,
                        10,
                        AIVoltageUnits.Volts);

                    // Configure the timing parameters
                    myTask.Timing.ConfigureSampleClock(
                        "", 
                        sampleRate,
                        SampleClockActiveEdge.Rising, 
                        SampleQuantityMode.ContinuousSamples, 
                        sampleCount);

                    // Verify the Task
                    myTask.Control(TaskAction.Verify);

                    //// Prepare the table for Data
                    //InitializeDataTable(myTask.AIChannels, ref dataTable);
                    //acquisitionDataGrid.DataSource = dataTable;

                    runningTask = myTask;
                    analogInReader = new AnalogMultiChannelReader(myTask.Stream);
                    analogCallback = new AsyncCallback(AnalogInCallback);

                    // Use SynchronizeCallbacks to specify that the object 
                    // marshals callbacks across threads appropriately.
                    analogInReader.SynchronizeCallbacks = true;
                    analogInReader.BeginReadWaveform(
                        sampleCount,
                        analogCallback, 
                        myTask);
                }
                catch (DaqException exception)
                {
                    // Display Errors
                    MessageBox.Show(exception.Message);
                    runningTask = null;
                    myTask.Dispose();

                }
            }
        }
        private void AnalogInCallback(IAsyncResult ar)
        {
            try
            {
                if (runningTask != null && runningTask == ar.AsyncState)
                {
                    Debug.Print("AnalogInCallback called");

                    // Read the available data from the channels
                    data = analogInReader.EndReadWaveform(ar);

                    // Plot your data here
                    //dataToDataTable(data, ref dataTable);

                    analogInReader.BeginMemoryOptimizedReadWaveform(
                        sampleCount,
                        analogCallback, 
                        myTask, 
                        data);

                    int len = data[0].SampleCount;
                    for (int i = 0; i < len; i++)
                    {
                        Debug.Print($"{data[0].Samples[i].Value}");
                    }
                }
            }
            catch (DaqException exception)
            {
                // Display Errors
                MessageBox.Show(exception.Message);
                runningTask = null;
                myTask.Dispose();
            }
        }


    }
}
