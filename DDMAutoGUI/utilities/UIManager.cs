using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDMAutoGUI.utilities
{    // Also handles events for robot state so GUI can be locked/unlocked accordingly


    public class UIState
    {

        public bool isConnected { get; set; }
        public bool isProcessWizardOpen { get; set; }
        public bool isAutoControllerStateRequesting { get; set; }

    }

    public sealed class UIManager
    {
        // singleton pattern (maybe not the best idea?)
        private static readonly Lazy<UIManager> lazy = new Lazy<UIManager>(() => new UIManager());
        public static UIManager Instance { get { return lazy.Value; } }

        public event EventHandler UIStateChanged;
        public UIState UI_STATE;

        public UIManager()
        {
            UI_STATE = new UIState
            {
                isConnected = false,
                isProcessWizardOpen = false,
                isAutoControllerStateRequesting = false
            };
        }

        public void TriggerUIStateChanged()
        {
            UIStateChanged?.Invoke(this, EventArgs.Empty);
        }

    }
}
