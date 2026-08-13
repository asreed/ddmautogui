using System;
using System.Threading.Tasks;

namespace DDMAutoGUI.Services
{
    public class DispenseExecutionService : IDispenseExecutionService
    {

        private readonly IControllerService _controllerService;

        public DispenseExecutionService(IControllerService controllerService)
        {
            _controllerService = controllerService;
        }

        public async Task EnablePowerAndHomeAsync(Action<string> log = null)
        {
            log?.Invoke("Enabling power...");
            string response = await _controllerService.EnablePower();
            if (response != "1")
            {
                throw new PartCycleException("Failed to enable power");
            }
            log?.Invoke("Power enabled");

            log?.Invoke("Homing...");
            response = await _controllerService.Home();
            if (response != "0")
            {
                throw new PartCycleException("Failed to home");
            }
            log?.Invoke("Homed");
        }

        public async Task SetupPressuresAsync(CellSettings settings, LDMotorCalib calib, Action<string> log = null)
        {
            float? pressure1 = calib.sys_1_pressure;
            float? pressure2 = calib.sys_2_pressure;

            if (pressure1 != null)
            {
                log?.Invoke($"Setting pressure for system 1 ({settings.dispense_system.sys_1_contents}) to {pressure1.Value:F3} psi");
                await _controllerService.SetRegPressure(1, pressure1.Value);
            }
            else
            {
                log?.Invoke($"No pressure change for system 1 ({settings.dispense_system.sys_1_contents})");
            }

            if (pressure2 != null)
            {
                log?.Invoke($"Setting pressure for system 2 ({settings.dispense_system.sys_2_contents}) to {pressure2.Value:F3} psi");
                await _controllerService.SetRegPressure(2, pressure2.Value);
            }
            else
            {
                log?.Invoke($"No pressure change for system 2 ({settings.dispense_system.sys_2_contents})");
            }

            log?.Invoke("Waiting for pressures to settle...");
            await _controllerService.WaitBothRegPressures(10);
            await Task.Delay(1000);
            log?.Invoke("Pressures settled");
            log?.Invoke("Zeroing flow sensors...");
            await _controllerService.SetZeroShift(3);
            log?.Invoke("Flow sensors zeroed");
        }

        public async Task<ResultsShotData> DispenseToRingAsync(CellSettings settings, CSMotor motor, string motorName, Action<string> log = null)
        {
            //int sysID = motor.shot_settings.id_sys_num.Value;
            //int sysOD = motor.shot_settings.od_sys_num.Value;
            //float xID = motor.id_disp.x.Value;
            //float tID = motor.id_disp.t.Value;
            //float xOD = motor.od_disp.x.Value;
            //float tOD = motor.od_disp.t.Value;
            //float targetTimeID = motor.shot_settings.id_target_vol.Value / motor.shot_settings.id_target_flow.Value;
            //float targetTimeOD = motor.shot_settings.od_target_vol.Value / motor.shot_settings.od_target_flow.Value;

            //log?.Invoke("Waiting for pressures to stabilize...");
            //await _controllerService.WaitBothRegPressures(5);
            //log?.Invoke("Pressures stabilized");

            //float pressureID = float.Parse(await _controllerService.GetRegPressureSetpoint(sysID));
            //float pressureOD = float.Parse(await _controllerService.GetRegPressureSetpoint(sysOD));

            //log?.Invoke("Dispensing cyanoacrylate...");
            //string response = await _controllerService.DispenseToRing(
            //    sysID, targetTimeID, xID, tID,
            //    sysOD, targetTimeOD, xOD, tOD);

            //ResultsShotData shotData = _controllerService.ParseDispenseResponse(response);
            //shotData.motor_type = motorName;
            //shotData.id_valve_num = sysID;
            //shotData.od_valve_num = sysOD;
            //shotData.id_pressure = pressureID;
            //shotData.od_pressure = pressureOD;

            //if (shotData.shot_result != true)
            //{
            //    log?.Invoke($"Dispense failed: {shotData.shot_message}");
            //    throw new PartCycleException($"Dispense failed: {shotData.shot_message}");
            //}

            //log?.Invoke("Dispense successful");
            //return shotData;

            return new ResultsShotData();
        }


        public async Task<ResultsShotData> DispenseSingleTrackToRingAsync(int valveIdx, float shotTime, float xPos, float tPos, int dir)
        {
            //await _controllerService.WaitBothRegPressures(5);
            //string response = await _controllerService.DispenseSingleTrackToRing(valveIdx, shotTime, xPos, tPos, dir);
            //ResultsShotData shotData = _controllerService.ParseDispenseResponse(response);
            //return shotData;

            return new ResultsShotData();
        }
    }
}