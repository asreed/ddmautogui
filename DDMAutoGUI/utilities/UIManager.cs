using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
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

    public class UIManager
    {

        public event EventHandler UIStateChanged;
        public UIState UI_STATE { get; set; } = new UIState();

        public UIManager()
        {
            UI_STATE = new UIState
            {
                isConnected = false,
                isProcessWizardOpen = false,
                isAutoControllerStateRequesting = false
            };

            Debug.Print("UI manager initialized");
        }

        public void TriggerUIStateChanged()
        {
            UIStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public string GetAppVersionString()
        {
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return $"{v.Major}.{v.Minor}.{v.Build}";
        }

    }
}
