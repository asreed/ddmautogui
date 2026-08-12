using System;
using System.Threading.Tasks;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Interface for the dispense process service.
    /// Orchestrates all steps of the dispense workflow.
    /// </summary>
    public interface IPartCycleService
    {
        event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        Task<PartCycleResult> ExecutePartCycleAsync(
            string motorName,
            string ringSerialNumber,
            AdvancedOptions advancedOptions);
    }
}
