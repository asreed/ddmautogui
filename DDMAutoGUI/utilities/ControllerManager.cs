using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Emit;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Documents;
using System.Web;



namespace DDMAutoGUI.utilities
{

    public class ControllerState
    {
        // general
        public bool parseError { get; set; }
        public string parseErrorMessage { get; set; }

        // from controller
        public bool isPowerEnabled { get; set; }
        public bool isRobotHomed { get; set; }
        public float posLinear { get; set; }
        public float posRotary { get; set; }
        public bool isLinearIn1 { get; set; }
        public bool isLinearIn2 { get; set; }
        public bool isLinearIn3 { get; set; }
        public float pressureCommand1 { get; set; }
        public float pressureMeasurement1 { get; set; }
        public float pressureCommand2 { get; set; }
        public float pressureMeasurement2 { get; set; }
        public float flowVolume1 { get; set; }
        public int flowError1 { get; set; }
        public float flowVolume2 { get; set; }
        public int flowError2 { get; set; }

        public void Initialize()
        {
            parseError = false;
            parseErrorMessage = string.Empty;
            isPowerEnabled = false;
            isRobotHomed = false;
            posLinear = 0.0f;
            posRotary = 0.0f;
            isLinearIn1 = false;
            isLinearIn2 = false;
            isLinearIn3 = false;
            pressureCommand1 = 0.0f;
            pressureMeasurement1 = 0.0f;
            pressureCommand2 = 0.0f;
            pressureMeasurement2 = 0.0f;
            flowVolume1 = 0.0f;
            flowError1 = 0;
            flowVolume2 = 0.0f;
            flowError2 = 0;
        }
    }

    public class ControllerManager
    {

        public string CORRECT_TCS_VERSION = "Tcs_ddm_cell_1_1_4"; // ???? ?????????????

        private string statusLog = string.Empty;
        private string robotLog = string.Empty;

        private double autoStatusInterval = 1.0; //sec
        private DispatcherTimer _timer;

        private string term = "\n";
        private int sendTimeout = 2000;
        private int receiveTimeout = 2000;

        private Socket statusClient;
        private Socket robotClient;

        public event EventHandler ControllerConnected;
        public event EventHandler ControllerDisconnected;
        public event EventHandler ControllerStateChanged;
        public event EventHandler StatusLogUpdated;
        public event EventHandler RobotLogUpdated;

        public ControllerState CONTROLLER_STATE { get; private set; } = new ControllerState();

        public ControllerManager()
        {
            CONTROLLER_STATE.Initialize();
            Debug.Print("Controller manager initialized");

        }









        // ==================================================================
        // Autoload TCS (port 23)

        public async Task<string> ReceiveConsoleHeader(Socket client)
        {

            StringBuilder response = new StringBuilder();
            try
            {
                Debug.Print("attempting to receive header");
                byte[] buffer = new byte[1024];
                int bytesRead;

                while (true)
                {
                    bytesRead = await client.ReceiveAsync(buffer);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    response.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                    Debug.Print($"building response: {response.ToString()}");
                    if (response.ToString().Trim().EndsWith(":") || response.ToString().Trim().EndsWith("..."))
                    {
                        break;
                    }

                }
                Debug.Print($"full response: {response.ToString()}");
            }
            catch (SocketException e)
            {
                Debug.Print("Send console command failed");
                Debug.Print($"{e.ErrorCode}: {e.Message}");
                response.Append("Error?");
            }
            return response.ToString();
        }

        public async Task<string> SendConsoleCmd(Socket client, string command)
        {
            byte[] commandBytes = Encoding.ASCII.GetBytes(command + term);
            StringBuilder response = new StringBuilder();
            try
            {
                Debug.Print($"c >> {command}");
                await client.SendAsync(commandBytes);
                byte[] buffer = new byte[1024];
                int bytesRead;

                while (true)
                {
                    bytesRead = await client.ReceiveAsync(buffer);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    response.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                    //Debug.Print($"building response: {response.ToString()}");
                    if (response.ToString().Trim().EndsWith(":"))
                    {
                        break;
                    }
                }
                Debug.Print($"c << {response.ToString()}");
            }
            catch (SocketException e)
            {
                Debug.Print("Send console command failed");
                Debug.Print($"{e.ErrorCode}: {e.Message}");
                response.Clear().Append("Failed");
            }
            return response.ToString();
        }

        public async Task<string> AttemptLoadTCS(string ip)
        {
            string reply;
            string response;
            IPEndPoint controllerEP = new IPEndPoint(IPAddress.Parse(ip), 23);
            Socket controllerClient = new Socket(controllerEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            controllerClient.SendTimeout = sendTimeout;
            controllerClient.ReceiveTimeout = receiveTimeout;
            try
            {
                await controllerClient.ConnectAsync(controllerEP);
                await ReceiveConsoleHeader(controllerClient);
                await SendConsoleCmd(controllerClient, "Help");
                response = await SendConsoleCmd(controllerClient, "show thread");
                if (response.Contains(CORRECT_TCS_VERSION))
                {
                    // correct TCS already running
                    Debug.Print("correct TCS running");
                    reply = "Correct TCS version already running";
                }
                else
                {
                    Debug.Print("incorrect or no TCS running. Attempting to load.");
                    response = await SendConsoleCmd(controllerClient, $"load flash/projects/{CORRECT_TCS_VERSION} -start");
                    if (response.Contains("-508"))
                    {
                        Debug.Print("correct TCS is not installed on controller");
                        reply = "Correct TCS NOT INSTALLED";
                    }
                    else if (response.Contains("-745"))
                    {
                        // project already loaded
                        response = await SendConsoleCmd(controllerClient, $"start {CORRECT_TCS_VERSION} -compile");
                        response = await SendConsoleCmd(controllerClient, "show thread");
                        if (response.Contains(CORRECT_TCS_VERSION))
                        {
                            // correct TCS is now running
                            reply = "Success. Correct TCS is running";



                        }
                        else
                        {
                            // tried to load program and now it's not running?
                            reply = "Attempted to load and run; not running?";
                        }

                    }
                    else if (response.Contains("Compile successful"))
                    {
                        // project apparently running
                        response = await SendConsoleCmd(controllerClient, "show thread");
                        if (response.Contains(CORRECT_TCS_VERSION))
                        {
                            // correct TCS is now running
                            reply = "Success. Correct TCS is running";



                        }
                        else
                        {
                            // tried to load program and now it's not running?
                            reply = "Attempted to load and run; not running?";
                        }
                    }
                    else
                    {
                        // unexpected response from compile command
                        reply = "Unexpected respones to compile request";
                    }
                }
                await SendConsoleCmd(controllerClient, "quit");
                controllerClient.Close();
                Debug.Print("end of attempt reached");
            }
            catch (SocketException e)
            {
                Debug.Print(($"{e.ErrorCode}: {e.Message}"));
                controllerClient.Close();
                reply = "Connection failure";
            }
            return reply;
        }




        // ==================================================================
        // General TCS messaging (port 10000 and 10100)

        public async Task<bool> ConnectAsync(string ip)
        {
            IPEndPoint statusEP = new IPEndPoint(IPAddress.Parse(ip), 10000);
            IPEndPoint robotEP = new IPEndPoint(IPAddress.Parse(ip), 10100);

            statusClient = new Socket(statusEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            robotClient = new Socket(robotEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            statusClient.SendTimeout = sendTimeout;
            statusClient.ReceiveTimeout = receiveTimeout;

            robotClient.SendTimeout = sendTimeout;
            robotClient.ReceiveTimeout = receiveTimeout;

            UpdateBothLogs($"Connecting to {ip}...");
            try
            {
                await statusClient.ConnectAsync(statusEP);
                await robotClient.ConnectAsync(robotEP);

                // It seems to be possible that the connection succeeds even for some
                // apparently random IP addresses... Need to send test command

                if (await TestStatusConnection() != "0")
                {
                    SocketException ex = new SocketException(-1, "Failed test command");
                    throw ex;
                }

                UpdateBothLogs("Connection succeeded");

                ControllerConnected?.Invoke(this, EventArgs.Empty);
                App.UIManager.UI_STATE.isConnected = true;
                App.UIManager.TriggerUIStateChanged();
                return true;

            }
            catch (SocketException e)
            {

                statusClient.Close();
                robotClient.Close();
                UpdateBothLogs("Connection failed");
                UpdateBothLogs($"{e.ErrorCode}: {e.Message}");

                ControllerDisconnected?.Invoke(this, EventArgs.Empty);
                App.UIManager.UI_STATE.isConnected = false;
                App.UIManager.TriggerUIStateChanged();
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            UpdateBothLogs("Disconnecting...");
            try
            {

                StopAutoControllerState();
                await SendStatusCommandAsync("exit");
                await SendRobotCommandAsync("exit");
                statusClient.Shutdown(SocketShutdown.Both);
                statusClient.Close();
                robotClient.Shutdown(SocketShutdown.Both);
                robotClient.Close();

            }
            catch (SocketException e)
            {
                UpdateBothLogs("Disconnection failed");
                UpdateBothLogs($"{e.ErrorCode}: {e.Message}");
            }

            ControllerDisconnected?.Invoke(this, EventArgs.Empty);
            App.UIManager.UI_STATE.isConnected = false;
            App.UIManager.TriggerUIStateChanged();
        }

        public async Task<string> SendRobotCommandAsync(string command)
        {
            UpdateRobotLog($">> {command}");

            byte[] commandBytes = Encoding.ASCII.GetBytes(command + term); //don't forget termination char
            StringBuilder response = new StringBuilder();

            try
            {
                await robotClient.SendAsync(commandBytes);

                byte[] buffer = new byte[1024];
                int bytesRead;
                int i = 0;

                while (true)
                {
                    bytesRead = await robotClient.ReceiveAsync(buffer);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    string partialResponse = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    response.Append(partialResponse);
                    if (partialResponse.Contains(term))
                    {
                        break;
                    }
                    i++;
                }

                UpdateRobotLog($"<< {response.ToString().Trim()}");
            }
            catch (SocketException e)
            {
                UpdateRobotLog("Send failed");
                UpdateRobotLog($"{e.ErrorCode}: {e.Message}");
                response = new StringBuilder();

                ControllerDisconnected?.Invoke(this, EventArgs.Empty);
                App.UIManager.UI_STATE.isConnected = false;
                App.UIManager.TriggerUIStateChanged();

            }
            return response.ToString().Trim();
        }

        public async Task<string> SendStatusCommandAsync(string command)
        {
            UpdateStatusLog($">> {command}");

            byte[] commandBytes = Encoding.ASCII.GetBytes(command + term); //don't forget termination char
            string response = string.Empty;

            try
            {
                await statusClient.SendAsync(commandBytes);
                byte[] buffer = new byte[1024];
                int receivedLength = await statusClient.ReceiveAsync(buffer);
                response = Encoding.ASCII.GetString(buffer, 0, receivedLength).Trim();
                UpdateStatusLog($"<< {response}");
            }
            catch (SocketException e)
            {
                UpdateStatusLog("Send failed");
                UpdateStatusLog($"{e.ErrorCode}: {e.Message}");
                response = string.Empty;

                ControllerDisconnected?.Invoke(this, EventArgs.Empty);
                App.UIManager.UI_STATE.isConnected = false;
                App.UIManager.TriggerUIStateChanged();
            }
            return response;
        }












        // ==================================================================
        // Public helpers

        public List<DDMResultsSingleHeight> ParseHeightData(string rawString)
        {
            string[] responseArray = rawString.Split(" ");
            string[] measurementString;
            List<DDMResultsSingleHeight> measurementList = new List<DDMResultsSingleHeight>();

            if (responseArray[0] == "0")
            {
                measurementString = responseArray[1].Split(";");
                for (int i = 0; i < measurementString.Length; i++)
                {
                    string[] singleMeasurement = measurementString[i].Split(",");
                    DDMResultsSingleHeight measurement = new DDMResultsSingleHeight
                    {
                        t = float.Parse(singleMeasurement[0]),
                        z = float.Parse(singleMeasurement[1])
                    };
                    measurementList.Add(measurement);
                }
            }
            return measurementList;
        }

        public string ParseHeightDataToString(List<DDMResultsSingleHeight> measurementList)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < measurementList.Count; i++)
            {
                sb.Append($"{measurementList[i].t}, {measurementList[i].z}\n");
            }
            return sb.ToString();
        }


        // ==================================================================
        // Private helpers

        private async Task<string> TestStatusConnection()
        {
            string response = await SendStatusCommandAsync("nop");
            return response.Trim();
        }

        private void UpdateControllerStateFromString(string newStatusString)
        {
            string[] status = newStatusString.Split(" ");
            if (status.Length > 1)
            {
                try
                {
                    CONTROLLER_STATE = new ControllerState
                    {
                        isPowerEnabled = status[1] != "0",
                        isRobotHomed = status[2] != "0",

                        posLinear = float.Parse(status[3]),
                        posRotary = float.Parse(status[4]),

                        isLinearIn1 = status[5] != "0",
                        isLinearIn2 = status[6] != "0",
                        isLinearIn3 = status[7] != "0",

                        pressureCommand1 = float.Parse(status[8]),
                        pressureMeasurement1 = float.Parse(status[9]),
                        pressureCommand2 = float.Parse(status[10]),
                        pressureMeasurement2 = float.Parse(status[11]),

                        flowVolume1 = float.Parse(status[12]),
                        flowError1 = int.Parse(status[13]),
                        flowVolume2 = float.Parse(status[14]),
                        flowError2 = int.Parse(status[15]),

                        parseError = false,
                        parseErrorMessage = "",
                    };
                }
                catch
                {
                    // error. likely version mismatch
                    CONTROLLER_STATE.Initialize();
                    CONTROLLER_STATE.parseError = true;
                    CONTROLLER_STATE.parseErrorMessage = "Error while parsing data";
                }
            }
            else
            {
                // error. likely error from controller
                CONTROLLER_STATE.Initialize();
                CONTROLLER_STATE.parseError = true;
                CONTROLLER_STATE.parseErrorMessage = $"Unable to parse: {status[0]}";
            }
            ControllerStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateStatusLog(string logLine)
        {
            statusLog += logLine + "\n";
            StatusLogUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateRobotLog(string logLine)
        {
            robotLog += logLine + "\n";
            RobotLogUpdated?.Invoke(this, EventArgs.Empty);

        }

        private void UpdateBothLogs(string logLine)
        {
            UpdateStatusLog(logLine);
            UpdateRobotLog(logLine);
        }



        // ==================================================================
        // Auto update for controller state start/stop

        public void StartAutoControllerState()
        {
            if (App.UIManager.UI_STATE.isConnected)
            {
                if (App.UIManager.UI_STATE.isAutoControllerStateRequesting == false)
                {
                    _timer = new DispatcherTimer();
                    _timer.Interval = TimeSpan.FromSeconds(autoStatusInterval);
                    _timer.Tick += Timer_Tick;
                    _timer.Start();

                    App.UIManager.UI_STATE.isAutoControllerStateRequesting = true;
                    App.UIManager.TriggerUIStateChanged();
                }
            }
        }

        public void StopAutoControllerState()
        {
            if (App.UIManager.UI_STATE.isConnected)
            {
                if (App.UIManager.UI_STATE.isAutoControllerStateRequesting == true)
                {
                    if (_timer != null)
                    {
                        _timer.Stop();
                        _timer.Tick -= Timer_Tick;
                        _timer = null;

                        App.UIManager.UI_STATE.isAutoControllerStateRequesting = false;
                        App.UIManager.TriggerUIStateChanged();
                    }
                }
            }
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            string response = await GetSystemStateRemote();
            UpdateControllerStateFromString(response);
        }



        // ==================================================================
        // Public state set/get methods

        public string GetStatusLog()
        {
            return statusLog;
        }

        public string GetRobotLog()
        {
            return robotLog;
        }

        public string GetCorrectTCSVersion()
        {
            return CORRECT_TCS_VERSION;
        }



        // ==================================================================
        // Public robot routines

        public async Task<string> GetSystemStateRemote()
        {
            string response = await SendStatusCommandAsync("DDM_GetSystemState");
            return response;
        }

        public async Task<string> GetTCSVersion()
        {
            string response = await SendStatusCommandAsync("DDM_GetTCSVersion");
            string version = string.Empty;
            if (response.Split(" ").Length > 1)
            {
                version = response.Split(" ")[1];
            }
            return version;
        }

        public async Task<string> GetPACVersion()
        {
            string response = await SendStatusCommandAsync("DDM_GetPACVersion");
            string[] fullversion = response.Split(" ");
            string version = string.Empty;
            if (fullversion.Length > 1)
            {
                version = string.Join(" ", fullversion.Skip(1));
            }
            return version;
        }

        public async Task<string> EStop()
        {
            await SendRobotCommandAsync($"halt");
            return await SendRobotCommandAsync($"hp 0");
        }

        public async Task<string> EnablePower()
        {
            string response;
            int timeout = 0;

            response = await SendRobotCommandAsync("hp 1");
            while (true)
            {
                response = await SendRobotCommandAsync("hp");
                if (response == "0 1")
                {
                    break;
                }
                timeout++;
                if (timeout > 20)
                {
                    response = "Timeout";
                    break;
                }
                await Task.Delay(500);
            }
            return response;
        }

        public async Task<string> Home()
        {
            string response = "not implemented yet";
            return response;
        }

        public async Task<string> MoveOneAxis(int axisNumber, float position)
        {
            string input = $"DDM_MoveOneAxis {axisNumber} {position}";
            return await SendRobotCommandAsync(input);
        }

        public async Task<string> MoveJ(float xPosition, float tPosition)
        {
            string input = $"DDM_MoveJ {xPosition} {tPosition}";
            return await SendRobotCommandAsync(input);
        }

        public async Task<string> SpinInPlace(float spinTime, float spinSpeed)
        {
            string input = $"DDM_SpinInPlace {spinTime} {spinSpeed}";
            return await SendRobotCommandAsync(input);
        }

        public async Task<string> OpenValveTimed(int index, float openTime)
        {
            string input = $"DDM_OpenValveTimed {index} {openTime}";
            return await SendRobotCommandAsync(input);
        }

        public async Task<string> CloseAllValves()
        {
            string input = $"DDM_CloseAllValves";
            return await SendRobotCommandAsync(input);
        }

        public async Task<string> SetRegPressure(int index, float pressure)
        {
            string input = $"DDM_SetRegPressure {index} {pressure}";
            return await SendRobotCommandAsync(input);
        }

        public async Task<string> SetRegPressureAndWait(int index, float pressure, float timeout)
        {
            string input = $"DDM_SetRegPressureAndWait {index} {pressure} {timeout}";
            return await SendRobotCommandAsync(input);
        }

        public async Task<string> GetRegPressure(int index)
        {
            string input = $"DDM_GetRegPressure {index}";
            return await SendRobotCommandAsync(input);
        }

        public async Task<string> GetRegPressureSetpoint(int index)
        {
            string input = $"DDM_GetRegPressureSetpoint {index}";
            return await SendRobotCommandAsync(input);
        }

        public async Task<string> MeasureHeights(float xPos, float tStart, int nMeasurements)
        {
            string input = $"DDM_MeasureHeights {xPos} {tStart} {nMeasurements}";
            return await SendRobotCommandAsync(input);
        }

        public async Task<string> SetZeroShift(float timeAvg)
        {
            string input = $"DDM_SetZeroShift {timeAvg}";
            return await SendRobotCommandAsync(input);
        }

        public async Task<string> SetShotTrigger(int index, bool state)
        {
            string input = $"DDM_SetShotTrigger {index} {state}";
            return await SendRobotCommandAsync(input);
        }

        public async Task<string> MeasureShotTimed(int index, float time)
        {
            string input = $"DDM_MeasureShotTimed {index} {time}";
            return await SendRobotCommandAsync(input);
        }

    }
}
