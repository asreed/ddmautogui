using DDMAutoGUI.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Controls;

namespace DDMAutoGUI.CustomWindows
{
    /// <summary>
    /// Interaction logic for CalibFlowPanel.xaml.
    /// Hosts one <see cref="CalibFlowSizePanel"/> per motor size; each child panel
    /// owns its own calibration logic. This host only populates shared header info.
    /// </summary>
    public partial class CalibFlowPanel : UserControl
    {
        private readonly ILocalDataManager _localDataManager;

        public CalibFlowPanel()
        {
            InitializeComponent();

            // Resolve services from DI container when instantiated via XAML
            _localDataManager = App.Services?.GetService<ILocalDataManager>();
        }

        public CalibFlowPanel(ILocalDataManager localDataManager) : this()
        {
            _localDataManager = localDataManager ?? throw new ArgumentNullException(nameof(localDataManager));
        }

        public void SetupPanel()
        {
            LocalData localData = _localDataManager.GetLocalData();
            Calib_LastCalTxb.Text = localData.calib_data.last_calib.Value.ToString();
            Calib_LastMotorTxb.Text = localData.calib_data.last_size;

            Calib_57Panel.SetupPanel();
            Calib_95Panel.SetupPanel();
            Calib_116Panel.SetupPanel();
            Calib_170Panel.SetupPanel();
            Calib_170TallPanel.SetupPanel();
        }
    }
}
