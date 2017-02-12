using Post_KNV_MessageClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Post_knv_Server
{
    /// <summary>
    /// logic for the server settingswindow
    /// </summary>
    public partial class ServerSettingsWindow : Window
    {
        public ServerSettingsWindow()
        {
            InitializeComponent();
            loadConfigData();
        }

        //events to catch
        public delegate void OnConfigChangeRequest();
        public event OnConfigChangeRequest OnConfigChangeEvent;

        /// <summary>
        /// if clicked, saves the configuration by calling saveData()
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Button_OK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.saveData();
                OnConfigChangeEvent();
                MessageBox.Show("In order to apply all settings, server application may need to be restarted.", "Restart may be required", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        /// <summary>
        /// saves the configuration
        /// </summary>
        private void saveData()
        {
            //ServerConfigObject
            Config.ServerConfigManager._ServerConfigObject.ownIP = this._Txtbox_OwnIP.Text;
            Config.ServerConfigManager._ServerConfigObject.gatewayAddress = this._Txtbox_gatewayAddress.Text;
            Config.ServerConfigManager._ServerConfigObject.signalrAddress = this._Txtbox_signalrAddress.Text;
            Config.ServerConfigManager._ServerConfigObject.keepAliveInterval = int.Parse(this._Txtbox_keepAliveInterval.Text);
            Config.ServerConfigManager._ServerConfigObject.listeningPort = int.Parse(this._Txtbox_listeningPort.Text);
            Config.ServerConfigManager._ServerConfigObject.debug = (bool)this._Checkbox_DebugLog.IsChecked;

            //ServerAlgorithmConfigObject serverAlgorithmConfig 
            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.downsamplingFactor = float.Parse(this._Txtbox_downsampleFactor.Text);
            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.amountOfFrames = int.Parse(this._Txtbox_amountOfFrames.Text);
            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.euclidean_ExtractionRadius = float.Parse(this._Txtbox_euclideanExtractionRadius.Text);
            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.euclidean_MinimumVolume = float.Parse(this._Txtbox_euclideanMinimumVolume.Text);
            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.ICP_perform = (bool)this._Checkbox_PerformICP.IsChecked;
            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.ICP_indis = int.Parse(this._Txtbox_indistValue.Text);
            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.planar_Iterations = int.Parse(this._Txtbox_planarIterations.Text);
            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.planar_ThresholdDistance = float.Parse(this._Txtbox_planarThresholdDistance.Text);
            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.planar_planeComparisonVarianceThreshold = float.Parse(this._Txtbox_planarComparisonValue.Text);
            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.useAlgorithm = (Algorithms)this._ComboBox_choseAlgorithm.SelectedItem;
            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.correctionValue = float.Parse(this._Txtbox_algorithmCorrectionValue.Text);
            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.concav_angleThreshold = float.Parse(this._Txtbox_concavThresholdAngle.Text);

            //ServerKinectFusionConfigObject serverKinectFusionConfig
            Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.ProcessorType = (Microsoft.Kinect.Fusion.ReconstructionProcessor)this._ComboBox_processorType.SelectedItem;
            Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.DeviceToUse = int.Parse(this._Txtbox_DeviceToUse.Text);
            Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.AutoResetReconstructionWhenLost = (bool)this._Checkbox_AutoResetReconstructionWhenLost.IsChecked;
            Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.translateResetPoseByMinDepthThreshold = (bool)this._Checkbox_translateResetPoseByMinDepthThreshold.IsChecked;
            Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.MaxTrackingErrors = int.Parse(this._Txtbox_maxTrackingErrors.Text);
            Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.depthHeight = int.Parse(this._Txtbox_depthHeight.Text);
            Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.depthWidth = int.Parse(this._Txtbox_depthWidth.Text);
            Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.minDepthClip = float.Parse(this._Txtbox_minDepth.Text);
            Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.maxDepthClip = float.Parse(this._Txtbox_maxDepth.Text);
            Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.VoxelsPerMeter = int.Parse(this._Txtbox_VoxelsPerMeter.Text);
            Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.VoxelResolutionX = int.Parse(this._Txtbox_VoxelResolutionX.Text);
            Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.VoxelResolutionY = int.Parse(this._Txtbox_VoxelResolutionY.Text);
            Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.VoxelResolutionZ = int.Parse(this._Txtbox_VoxelResolutionZ.Text);
            Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.integrationWeight = int.Parse(this._Txtbox_integrationWeight.Text);
            Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.iterationCount = int.Parse(this._Txtbox_iterationCount.Text);            

            DataManager.DataManager.getInstance().saveConfig();
        }


        /// <summary>
        /// cancels the saving
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// loads the config and writes it into the window
        /// </summary>
        private void loadConfigData()
        {
            //ServerConfig
            this._Txtbox_OwnIP.Text = Config.ServerConfigManager._ServerConfigObject.ownIP;
            this._Checkbox_DebugLog.IsChecked = Config.ServerConfigManager._ServerConfigObject.debug;
            this._Txtbox_gatewayAddress.Text = Config.ServerConfigManager._ServerConfigObject.gatewayAddress;
            this._Txtbox_signalrAddress.Text = Config.ServerConfigManager._ServerConfigObject.signalrAddress;

            //ServerConnectionConfig
            this._Txtbox_listeningPort.Text = Config.ServerConfigManager._ServerConfigObject.listeningPort.ToString();
            this._Txtbox_keepAliveInterval.Text = Config.ServerConfigManager._ServerConfigObject.keepAliveInterval.ToString();

            //ServerKinectConfig
            this._ComboBox_processorType.ItemsSource = Enum.GetValues(typeof(Microsoft.Kinect.Fusion.ReconstructionProcessor));
            this._ComboBox_processorType.SelectedItem = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.ProcessorType;
            this._Txtbox_DeviceToUse.Text = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.DeviceToUse.ToString();
            this._Checkbox_AutoResetReconstructionWhenLost.IsChecked = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.AutoResetReconstructionWhenLost;
            this._Checkbox_translateResetPoseByMinDepthThreshold.IsChecked = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.translateResetPoseByMinDepthThreshold;
            this._Txtbox_maxTrackingErrors.Text = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.MaxTrackingErrors.ToString();
            this._Txtbox_depthHeight.Text = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.depthHeight.ToString();
            this._Txtbox_depthWidth.Text = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.depthWidth.ToString();
            this._Txtbox_minDepth.Text = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.minDepthClip.ToString();
            this._Txtbox_maxDepth.Text = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.maxDepthClip.ToString();
            this._Txtbox_VoxelsPerMeter.Text = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.VoxelsPerMeter.ToString();
            this._Txtbox_VoxelResolutionX.Text = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.VoxelResolutionX.ToString();
            this._Txtbox_VoxelResolutionY.Text = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.VoxelResolutionY.ToString();
            this._Txtbox_VoxelResolutionZ.Text = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.VoxelResolutionZ.ToString();
            this._Txtbox_integrationWeight.Text = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.integrationWeight.ToString();
            this._Txtbox_iterationCount.Text = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.iterationCount.ToString();

            //Algorithm Config
            this._Txtbox_downsampleFactor.Text = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.downsamplingFactor.ToString();
            this._Txtbox_amountOfFrames.Text = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.amountOfFrames.ToString();
            this._Txtbox_euclideanExtractionRadius.Text = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.euclidean_ExtractionRadius.ToString();
            this._Txtbox_euclideanMinimumVolume.Text = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.euclidean_MinimumVolume.ToString();
            this._Checkbox_PerformICP.IsChecked = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.ICP_perform;
            this._Txtbox_indistValue.Text = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.ICP_indis.ToString();
            this._Txtbox_planarIterations.Text = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.planar_Iterations.ToString();
            this._Txtbox_planarThresholdDistance.Text = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.planar_ThresholdDistance.ToString();
            this._Txtbox_planarComparisonValue.Text = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.planar_planeComparisonVarianceThreshold.ToString();
            this._ComboBox_choseAlgorithm.ItemsSource = Enum.GetValues(typeof(Algorithms));
            this._ComboBox_choseAlgorithm.SelectedItem = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.useAlgorithm;
            this._Txtbox_algorithmCorrectionValue.Text = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.correctionValue.ToString();
            this._Txtbox_concavThresholdAngle.Text = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.concav_angleThreshold.ToString();
        }

    }
}
