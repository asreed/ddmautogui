using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Interface for the controller service.
    /// Handles all communication with the workcell controller.
    /// </summary>
    public interface IControllerService
    {
        #region Events

        event EventHandler ControllerConnected;
        event EventHandler ControllerDisconnected;
        event EventHandler ControllerStateChanged;
        event EventHandler ConnectionLogUpdated;
        event EventHandler StatusLogUpdated;
        event EventHandler RobotLogUpdated;
        event EventHandler ConnectionStateChanged;
        event EventHandler RobotBusyChanged;

        #endregion

        #region Properties

        ControllerState CONTROLLER_STATE { get; }
        ControllerConnState CONNECTION_STATE { get; }

        /// <summary>True while one or more robot commands are in flight.</summary>
        bool IsRobotBusy { get; }

        #endregion

        #region Connection Management

        Task<bool> Connect(string ip);
        Task Disconnect();
        Task<string> AttemptLoadTCS(string ip);

        #endregion

        #region Communication

        Task<string> SendRobotCommand(string command);
        Task<string> SendStatusCommand(string command);
        Task<string> SendStatusCommand(string command, bool muteLog);
        Task<string> ReceiveConsoleHeader(System.Net.Sockets.Socket client);
        Task<string> SendConsoleCmd(System.Net.Sockets.Socket client, string command);

        #endregion

        #region System Health & Status

        Task<HealthResult> CheckSystemHealth();
        Task<string> GetSystemStateRemote(bool muteLog);
        Task<string> GetIOLinkStatusRemote();
        Task<string> TestLaserConnection();
        Task<string> GetTCSVersion();
        Task<string> GetPACVersion();

        #endregion

        #region Power & Robot Control

        Task<string> EnablePower();
        Task<string> Home();
        Task<string> EStop();
        Task<string> LightsOn();
        Task<string> LightsOff();

        #endregion

        #region Movement & Position

        Task<string> MoveOneAxis(int axisNumber, float position);
        Task<string> MoveJ(float xPosition, float tPosition);
        Task<string> SpinInPlace(float spinTime, float spinSpeed);
        Task<string> CalibratePosition();

        #endregion

        #region Valve & Pressure Control

        Task<string> OpenValveTimed(int index, float openTime);
        Task<string> CloseAllValves();
        Task<string> SetRegPressure(int index, float pressure);
        Task<string> SetRegPressureAndWait(int index, float pressure, float timeout);
        Task<string> SetBothRegPressureAndWait(float pressure1, float pressure2, float timeout);
        Task<string> WaitBothRegPressures(float timeout);
        Task<string> GetRegPressure(int index);
        Task<string> GetRegPressureSetpoint(int index);

        #endregion

        #region Measurement & Data Collection

        Task<string> MeasureHeightSingle();
        Task<string> MeasureHeights(float xPos, float tStart, int nMeasurements, float delay);
        Task<string> MeasureHeightsContinuous(float xPos, float tPos, int nSamples, float spinSpeed);
        Task<string> SetZeroShift(float timeAvg);
        Task<string> SetShotTrigger(int index, bool state);
        Task<string> MeasureShotTimed(int index, float time);
        Task<string> ActuateHallUp();
        Task<string> ActuateHallDown();

        #endregion

        #region Dispense Operations

        Task<string> DispenseToRing(
            int id_sys_num,
            float id_time,
            float id_xPos,
            float id_tPos,
            int od_sys_num,
            float od_time,
            float od_xPos,
            float od_tPos);

        #endregion

        #region Data Parsing & Conversion

        List<ResultsHeightMeasurement> ParseHeightData(string rawString);
        string ParseHeightDataToString(List<ResultsHeightMeasurement> measurementList);
        ResultsShotData ParseDispenseResponse(string response);
        IOLinkStatus ParseIOLinkStatus(string ioLinkString);
        List<ResultsHeightMeasurement> GetSimulatedHeightData(int nMeasurements);

        #endregion

        #region Auto Update Control

        void StartAutoControllerState();
        void StopAutoControllerState();

        #endregion

        #region Logging & Information

        string GetConnectionLog();
        string GetStatusLog();
        string GetRobotLog();
        string GetCorrectTCSVersion();

        #endregion
    }
}