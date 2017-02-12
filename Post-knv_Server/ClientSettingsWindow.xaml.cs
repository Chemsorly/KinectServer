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
    /// Logic for ClientSettingsWindow.xaml
    /// </summary>
    public partial class ClientSettingsWindow : Window
    {
        /// <summary>
        /// holding an object for the current config
        /// </summary>
        ClientConfigObject currentConfig;

        public delegate void OnConfigChangeRequest(ClientConfigObject config);
        public event OnConfigChangeRequest OnConfigChangeEvent;

        /// <summary>
        /// constructor of the window
        /// </summary>
        /// <param name="pCco">the config to be changed</param>
        public ClientSettingsWindow(ClientConfigObject pCco)
        {
            InitializeComponent();
            currentConfig = pCco;
            loadConfigData();
        }

        /// <summary>
        /// loads the config data from the object and writes it into the window
        /// </summary>
        private void loadConfigData( )
        {
            //ClientConfig
            this._Txtbox_OwnIP.Text = currentConfig.ownIP.ToString();
            this._Txtbox_ID.Text = currentConfig.ID.ToString();
            this._Txtbox_name.Text = currentConfig.name.ToString();
            this._Checkbox_DebugLog.IsChecked = currentConfig.debug;
            
            //ClientConnectionConfig
            this._Txtbox_targetIP.Text = currentConfig.clientConnectionConfig.targetIP.ToString();
            this._Txtbox_gatewayIP.Text = currentConfig.clientConnectionConfig.targetGateway.ToString();
            this._Txtbox_listeningPort.Text = currentConfig.clientConnectionConfig.listeningPort.ToString();

            //ClientKinectConfig
            this._Txtbox_minDepth.Text = currentConfig.clientKinectConfig.minDepth.ToString();
            this._Txtbox_maxDepth.Text = currentConfig.clientKinectConfig.maxDepth.ToString();
            this._Txtbox_min_X_Depth.Text = currentConfig.clientKinectConfig.xMinDepth.ToString();
            this._Txtbox_max_X_Depth.Text = currentConfig.clientKinectConfig.xMaxDepth.ToString();
            this._Txtbox_min_Y_Depth.Text = currentConfig.clientKinectConfig.yMinDepth.ToString();
            this._Txtbox_max_Y_Depth.Text = currentConfig.clientKinectConfig.yMaxDepth.ToString();

            //transformation matrix
            this._Txtbox_Trans_00.Text = currentConfig.clientKinectConfig.transformationMatrix[0, 0].ToString();
            this._Txtbox_Trans_10.Text = currentConfig.clientKinectConfig.transformationMatrix[1, 0].ToString();
            this._Txtbox_Trans_20.Text = currentConfig.clientKinectConfig.transformationMatrix[2, 0].ToString();
            this._Txtbox_Trans_30.Text = currentConfig.clientKinectConfig.transformationMatrix[3, 0].ToString();

            this._Txtbox_Trans_01.Text = currentConfig.clientKinectConfig.transformationMatrix[0, 1].ToString();
            this._Txtbox_Trans_11.Text = currentConfig.clientKinectConfig.transformationMatrix[1, 1].ToString();
            this._Txtbox_Trans_21.Text = currentConfig.clientKinectConfig.transformationMatrix[2, 1].ToString();
            this._Txtbox_Trans_31.Text = currentConfig.clientKinectConfig.transformationMatrix[3, 1].ToString();

            this._Txtbox_Trans_02.Text = currentConfig.clientKinectConfig.transformationMatrix[0, 2].ToString();
            this._Txtbox_Trans_12.Text = currentConfig.clientKinectConfig.transformationMatrix[1, 2].ToString();
            this._Txtbox_Trans_22.Text = currentConfig.clientKinectConfig.transformationMatrix[2, 2].ToString();
            this._Txtbox_Trans_32.Text = currentConfig.clientKinectConfig.transformationMatrix[3, 2].ToString();

            this._Txtbox_Trans_03.Text = currentConfig.clientKinectConfig.transformationMatrix[0, 3].ToString();
            this._Txtbox_Trans_13.Text = currentConfig.clientKinectConfig.transformationMatrix[1, 3].ToString();
            this._Txtbox_Trans_23.Text = currentConfig.clientKinectConfig.transformationMatrix[2, 3].ToString();
            this._Txtbox_Trans_33.Text = currentConfig.clientKinectConfig.transformationMatrix[3, 3].ToString(); 
        }

        /// <summary>
        /// saves the new changed data in the object
        /// </summary>
        private void saveData()
        {
            //ClientConfig
            currentConfig.ownIP = this._Txtbox_OwnIP.Text;
            currentConfig.ID = int.Parse(this._Txtbox_ID.Text);
            currentConfig.name = this._Txtbox_name.Text;
            currentConfig.debug = (bool)this._Checkbox_DebugLog.IsChecked;

            //ClientConnectionConfig
            currentConfig.clientConnectionConfig.targetIP = this._Txtbox_targetIP.Text;
            currentConfig.clientConnectionConfig.listeningPort = int.Parse(this._Txtbox_listeningPort.Text);
            currentConfig.clientConnectionConfig.targetGateway = this._Txtbox_gatewayIP.Text;

            //ClientKinectConfig
            currentConfig.clientKinectConfig.minDepth = ushort.Parse(this._Txtbox_minDepth.Text);
            currentConfig.clientKinectConfig.maxDepth = ushort.Parse(this._Txtbox_maxDepth.Text);
            currentConfig.clientKinectConfig.xMinDepth = int.Parse(this._Txtbox_min_X_Depth.Text);
            currentConfig.clientKinectConfig.xMaxDepth = int.Parse(this._Txtbox_max_X_Depth.Text);
            currentConfig.clientKinectConfig.yMinDepth = int.Parse(this._Txtbox_min_Y_Depth.Text);
            currentConfig.clientKinectConfig.yMaxDepth = int.Parse(this._Txtbox_max_Y_Depth.Text);

            //Transformation matrix
            currentConfig.clientKinectConfig.transformationMatrix[0, 0] = double.Parse(this._Txtbox_Trans_00.Text.Replace('.',','));
            currentConfig.clientKinectConfig.transformationMatrix[1, 0] = double.Parse(this._Txtbox_Trans_10.Text.Replace('.', ','));
            currentConfig.clientKinectConfig.transformationMatrix[2, 0] = double.Parse(this._Txtbox_Trans_20.Text.Replace('.', ','));
            currentConfig.clientKinectConfig.transformationMatrix[3, 0] = double.Parse(this._Txtbox_Trans_30.Text.Replace('.', ','));

            currentConfig.clientKinectConfig.transformationMatrix[0, 1] = double.Parse(this._Txtbox_Trans_01.Text.Replace('.', ','));
            currentConfig.clientKinectConfig.transformationMatrix[1, 1] = double.Parse(this._Txtbox_Trans_11.Text.Replace('.', ','));
            currentConfig.clientKinectConfig.transformationMatrix[2, 1] = double.Parse(this._Txtbox_Trans_21.Text.Replace('.', ','));
            currentConfig.clientKinectConfig.transformationMatrix[3, 1] = double.Parse(this._Txtbox_Trans_31.Text.Replace('.', ','));

            currentConfig.clientKinectConfig.transformationMatrix[0, 2] = double.Parse(this._Txtbox_Trans_02.Text.Replace('.', ','));
            currentConfig.clientKinectConfig.transformationMatrix[1, 2] = double.Parse(this._Txtbox_Trans_12.Text.Replace('.', ','));
            currentConfig.clientKinectConfig.transformationMatrix[2, 2] = double.Parse(this._Txtbox_Trans_22.Text.Replace('.', ','));
            currentConfig.clientKinectConfig.transformationMatrix[3, 2] = double.Parse(this._Txtbox_Trans_32.Text.Replace('.', ','));

            currentConfig.clientKinectConfig.transformationMatrix[0, 3] = double.Parse(this._Txtbox_Trans_03.Text.Replace('.', ','));
            currentConfig.clientKinectConfig.transformationMatrix[1, 3] = double.Parse(this._Txtbox_Trans_13.Text.Replace('.', ','));
            currentConfig.clientKinectConfig.transformationMatrix[2, 3] = double.Parse(this._Txtbox_Trans_23.Text.Replace('.', ','));
            currentConfig.clientKinectConfig.transformationMatrix[3, 3] = double.Parse(this._Txtbox_Trans_33.Text.Replace('.', ','));
        }

        /// <summary>
        /// try to save the configuration; throws message box if it fails
        /// </summary>
        private void _Button_OK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.saveData();
                OnConfigChangeEvent.BeginInvoke(currentConfig,null,null);
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        /// <summary>
        /// cancels the saving
        /// </summary>
        private void _Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
