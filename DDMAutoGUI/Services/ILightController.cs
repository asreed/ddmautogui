namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Interface for controlling workcell lights.
    /// Extracted to break circular dependency between ControllerManager and CameraManager.
    /// </summary>
    public interface ILightController
    {
        Task<string> LightsOn();
        Task<string> LightsOff();
    }
}