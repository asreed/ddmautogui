using System;
using System.Threading.Tasks;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Interface for the dispense process service.
    /// Orchestrates all steps of the dispense workflow.
    /// </summary>
    public interface IDispenseProcessService
    {
        event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        event EventHandler<ProcessStepEventArgs> StepChanged;

        Task<DispenseProcessResult> ExecuteFullDispenseProcessAsync(
            string motorName,
            string ringSerialNumber,
            AdvancedOptions advancedOptions);
    }
}
