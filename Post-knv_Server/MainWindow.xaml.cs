using RequestType = Post_KNV_MessageClasses.ClientConfigObject.RequestType;
using ScanResultPackage = Post_KNV_MessageClasses.ScanResultPackage;
using Post_knv_Server.Log;
using Post_knv_Server.Properties;
using Post_knv_Server.Webservice;
using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Post_KNV_MessageClasses;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;


namespace Post_knv_Server
{
    /// <summary>
    /// Interactionlogic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DataManager.DataManager _DataManager;
        List<KinectStatusPackage> statusList;
        Dictionary<int, WriteableBitmap> lastFusionPictures;

        //the bools for the calibration progress grid
        Boolean masterSet { get; set; }
        Boolean referenceTableSet { get; set; }
        Boolean planesSet { get; set; }
        

        /// <summary>
        /// program starting point and init of the mainWindow
        /// </summary>
        public MainWindow()
        {
            //startup point
            if (!IsRunAsAdministrator())
            {
                var processInfo = new ProcessStartInfo(Assembly.GetExecutingAssembly().CodeBase);

                // The following properties run the new process as administrator
                processInfo.UseShellExecute = true;
                processInfo.Verb = "runas";

                // Start the new process
                try
                {
                    Process.Start(processInfo);
                }
                catch (Exception)
                {
                    // The user did not allow the application to run as administrator
                    MessageBox.Show("Sorry, this application must be run as Administrator.");
                }

                // Shut down the current process
                Application.Current.Shutdown();
                return;
            }

            InitializeComponent();
            if (IsRunAsAdministrator()) writeConsole("[Application] Elevated to admin rights.");

            LogManager.OnLogMessageEvent += LogManager_OnLogMessageEvent;
            LogManager.OnClientStatusChangeEvent += UpdateClientConnectionSatus;
            LogManager.OnLogMessageDebugEvent += LogManager_OnLogMessageDebugEvent;
            LogManager.OnServerStatusChangeEvent += LogManager_OnAlgorithmStatusChangeEvent;
            LogManager.OnClientKinectStatusChangeEvent += LogManager_OnClientKinectStatusChangeEvent;

            _DataManager = DataManager.DataManager.getInstance();
            _DataManager.OnNewFusionPictureEvent += _DataManager_OnNewFusionPictureEvent;
            _DataManager.OnNewPointPicturesEvent += _DataManager_OnNewPointPicturesEvent;
            _DataManager.OnNewScanResultEvent += _DataManager_OnNewScanResultEvent;
            _DataManager.initialize();

            lastFusionPictures = new Dictionary<int, WriteableBitmap>();

            this.statusList = new List<KinectStatusPackage>();

            masterSet = false;
            referenceTableSet = false;
            planesSet = false;

            UpdateClientConnectionSatus();
        }

        /// <summary>
        /// checks if the application is run as admin
        /// </summary>
        /// <returns>true if run as admin</returns>
        private bool IsRunAsAdministrator()
        {
            var wi = WindowsIdentity.GetCurrent();
            var wp = new WindowsPrincipal(wi);

            return wp.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// gets called whenever a new scan event has been thrown. opens up a new result window
        /// </summary>
        /// <param name="pPackage">the result package</param>
        void _DataManager_OnNewScanResultEvent(ScanResultPackage pPackage)
        {
            this.Dispatcher.Invoke(() =>
            {
                ScanResultWindow srw = new ScanResultWindow(pPackage);
                srw.Show();
            });
        }

        /// <summary>
        /// gets called whenever the kinects report a new status update
        /// </summary>
        /// <param name="pKSP">the package containing the status info</param>
        void LogManager_OnClientKinectStatusChangeEvent(KinectStatusPackage pKSP)
        {
            statusList.RemoveAll(t => t.clientID == pKSP.clientID);
            statusList.Add(pKSP);
            UpdateClientConnectionSatus();
        }


        /// <summary>
        /// updates the current algorithm status
        /// </summary>
        /// <param name="pStatus">current algorithm</param>
        void LogManager_OnAlgorithmStatusChangeEvent(String pStatus)
        {
            this.Dispatcher.Invoke(() =>
                {
                    // Create and configure a simple color animation sequence.  Timespan is in 100ns ticks.
                    ColorAnimation blackToWhite = new ColorAnimation(Colors.DarkGray, Colors.Black, new Duration(new TimeSpan(10000000)));
                    blackToWhite.AutoReverse = true;
                    blackToWhite.RepeatBehavior = RepeatBehavior.Forever;

                    // Create a new brush and apply the color animation.
                    SolidColorBrush scb = new SolidColorBrush(Colors.Black);
                    scb.BeginAnimation(SolidColorBrush.ColorProperty, blackToWhite);

                    // Create a new TextEffect object; set foreground brush to the previously created brush.
                    TextEffect tfe = new TextEffect();
                    tfe.Foreground = scb;
                    // Range of text to apply effect to (all).
                    tfe.PositionStart = 0;
                    tfe.PositionCount = int.MaxValue;

                    // adds the blinking to the current algorithm 
                    this._Txtblck_CurrentAlgorithm.TextEffects = new TextEffectCollection();
                    this._Txtblck_CurrentAlgorithm.TextEffects.Add(tfe);
                    this._Txtblck_CurrentAlgorithm.Text = "Current Algorithm: " + pStatus;

                    // removes the blinking of the current algorithm when done
                    if(pStatus.Contains("Done")){
                        this._Txtblck_CurrentAlgorithm.TextEffects.Remove(tfe);
                    }
                });
        }

        /// <summary>
        /// gets called every time the log manager broadcasts a new debug message event
        /// </summary>
        /// <param name="message">the message</param>
        private void LogManager_OnLogMessageDebugEvent(string message)
        {
            if (Config.ServerConfigManager._ServerConfigObject.debug)
                writeConsole(message);
        }

        /// <summary>
        /// updates the new fusion picture
        /// </summary>
        /// <param name="fusionPicture">the fusion picture</param>
        void _DataManager_OnNewFusionPictureEvent(int pID, WriteableBitmap fusionPicture)
        {
            this.Dispatcher.Invoke(() =>
                {
                    this.lastFusionPictures[pID] = fusionPicture;
                    this._FusionImage.Source = fusionPicture;
                });           
        }

        /// <summary>
        /// updates the point pictures
        /// </summary>
        /// <param name="pointPictures">the point pictures</param>
        void _DataManager_OnNewPointPicturesEvent(DataIntegration.PointCloudDrawing.PointCloudPicturePackage pointPictures)
        {
            this.Dispatcher.Invoke(() =>
                {
                    this._PointImage_Front.Source = pointPictures.frontview;
                    this._PointImage_Bottom.Source = pointPictures.bottomview;
                    this._PointImage_Side.Source = pointPictures.sideview;
                });
        }

        /// <summary>
        /// delivers messages from the logmanager to the console
        /// </summary>
        /// <param name="message">the message</param>
        void LogManager_OnLogMessageEvent(String message)
        {
            writeConsole(message);
        }

        /// <summary>
        /// writes something in the console without the logManager
        /// </summary>
        /// <param name="message">the output message</param>
        void writeConsole(String message)
        {
            this.Dispatcher.Invoke(() =>
            {
                _Console.Text = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + " \n" + _Console.Text;
            });
        }

        /// <summary>
        /// updates the visible list of clients in the ConnectionStatusListView and
        /// implements the logic for the listview
        /// </summary>
        private void UpdateClientConnectionSatus()
        {
            List<ClientConfigObject> connectedClients = _DataManager.getCurrentKinectClients();
            this.Dispatcher.Invoke(() =>            {

                //connection status
                ImageSource conStatOrig, kinStatOrig;

                ConnectionStatusListView.Items.Clear();
                foreach (ClientConfigObject cco in connectedClients)
                {
                    //mute button
                    ImageSource bgm;
                    if (cco.clientRequestObject.isMuted) bgm = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/video_256_orange.png"));
                    else bgm = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/video_256_green.png"));

                    // kinect connection status
                    if (!statusList.Exists(t => t.clientID == cco.ID))
                    {
                        if (cco.clientRequestObject.isConnected)
                        {
                            conStatOrig = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/link2_256_green.png"));
                            kinStatOrig = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/disconnect_256_orange.png"));
                        }
                        else
                        {
                            conStatOrig = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/broken_link_256_orange.png"));
                            kinStatOrig = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/disconnect_256_orange.png"));
                        }
                    }
                    else
                    {
                        KinectStatusPackage kSp = statusList.Find(t => t.clientID == cco.ID);
                        if (cco.clientRequestObject.isConnected && kSp.isKinectActive)
                        {
                            conStatOrig = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/link2_256_green.png"));
                            kinStatOrig = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/connect_256_green.png"));

                        }
                        else if (cco.clientRequestObject.isConnected && !kSp.isKinectActive)
                        {
                            conStatOrig = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/link2_256_green.png"));
                            kinStatOrig = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/disconnect_256_orange.png"));
                        }
                        else
                        {
                            conStatOrig = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/broken_link_256_orange.png"));
                            kinStatOrig = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/disconnect_256_orange.png"));
                        }
                    }

                    //add item
                    ConnectionStatusListviewItem cl = new ConnectionStatusListviewItem()
                    {
                        CCO = cco,
                        ID = cco.ID,
                        name = cco.name,
                        IP = cco.ownIP,
                        kinStatus = kinStatOrig,
                        conStatus = conStatOrig,
                        master = cco.clientKinectConfig.isMaster,
                        mute = cco.clientRequestObject.isMuted,
                        backgroundMute = bgm,
                    };
                    ConnectionStatusListView.Items.Add(cl);

                }

                //check for calibrated planes
                planesSet = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.calibratedPlanes.Exists(testc => testc.isFloor == true);
                if (planesSet == true) this._imagePlanesSet.Source = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/checkmark_256_green.png"));
                else this._imagePlanesSet.Source = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/cross_256_orange.png"));

                //check for master
                masterSet = connectedClients.Exists(t => t.clientKinectConfig.isMaster == true);
                if (masterSet == true) this._imageMasterKinectSet.Source = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/checkmark_256_green.png"));
                else this._imageMasterKinectSet.Source = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/cross_256_orange.png"));

                //check for reference table
                if (connectedClients.Count == 0)
                    referenceTableSet = false;
                else
                    referenceTableSet = connectedClients.Where(t => t.clientKinectConfig.isMaster == false).All(t =>
                            t.clientKinectConfig.transformationMatrix[0, 0] != 1 ||
                            t.clientKinectConfig.transformationMatrix[1, 1] != 1 ||
                            t.clientKinectConfig.transformationMatrix[2, 2] != 1);

                if(referenceTableSet)
                    this._imageReferenceTableSet.Source = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/checkmark_256_green.png"));
                else
                    this._imageReferenceTableSet.Source = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/cross_256_orange.png"));

                //logic for the usability of the calibration buttons
                if ((masterSet == true && referenceTableSet == true) || connectedClients.Count == 0) this._ButtonFetchData.IsEnabled = true;
                else this._ButtonFetchData.IsEnabled = false;
                    
                if (masterSet == true || connectedClients.Count == 0) this._ButtonCalibrateAll.IsEnabled = true;
                else this._ButtonCalibrateAll.IsEnabled = false;

                if ((masterSet == true && planesSet == true && referenceTableSet == true) || connectedClients.Count == 0) this._ButtonScan.IsEnabled = true;
                else this._ButtonScan.IsEnabled = false;
                    
                if ((_DataManager.currentReferenceCloud != null && masterSet == true || connectedClients.Count == 0)) this._ButtonSelectPlanes.IsEnabled = true;
                else this._ButtonSelectPlanes.IsEnabled = false;

                if ((Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.calibratedPlanes.Count > 0 && _DataManager.currentReferenceCloud != null && masterSet == true) || connectedClients.Count == 0) this._ButtonEuclidianScan.IsEnabled = true;
                else this._ButtonEuclidianScan.IsEnabled = false;                
            });
        }

        /// <summary>
        /// class for listview ConnectionStatusListViewItem
        /// </summary>
        class ConnectionStatusListviewItem
        {
            public ClientConfigObject CCO { get; set; }
            public int ID {get; set;}
            public string name {get; set;}
            public string IP {get; set;}
            public bool mute { get; set; }
            public ImageSource conStatus { get; set; }
            public ImageSource kinStatus { get; set; }
            public bool master { get; set; }
            public ImageSource backgroundMute { get; set; }
        }

        /// <summary>
        /// gets called when the shutdown button is clicked. disconnects the client and tells him to stop sending pings
        /// </summary>
        /// <param name="sender">the button</param>
        /// <param name="e">unused params</param>
        private void _ButtonShutdown_Click(object sender, RoutedEventArgs e)
        {
            ConnectionStatusListviewItem obj = ((FrameworkElement)sender).DataContext as ConnectionStatusListviewItem;
            if (obj.CCO.clientRequestObject.isConnected)
            {
                MessageBoxResult res = MessageBox.Show("Do you really want to shutdown client \"" + obj.name + "\" ?", "Shutdown client?", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    _DataManager.shutdownClient(obj.ID);
                    ((Button)sender).Content = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/impressions_general_play_256.png"));
                }
            }
            else
            {
                MessageBoxResult res = MessageBox.Show("Do you want to start client \"" + obj.name + "\" ?", "Startup client?", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    _DataManager.shutdownClient(obj.ID);
                    ((Button)sender).Content = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/impressions_general_stop_256.png"));
                }
            }
        }

        /// <summary>
        /// gets called when the delete button is pressed. removes the client from the database
        /// </summary>
        /// <param name="sender">the button</param>
        /// <param name="e">unused params</param>
        private void _ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            ConnectionStatusListviewItem obj = ((FrameworkElement)sender).DataContext as ConnectionStatusListviewItem;

            MessageBoxResult res = MessageBox.Show("Do you really want to delete client \"" + obj.name + "\" from database?", "Delete client from database?", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes) _DataManager.deleteClientFromDatabase(obj.ID);

            // update the progress bar if it was the master kinect that was deleted
            if(obj.master == true)
            {
                masterSet = false;
                UpdateClientConnectionSatus();
            }
            UpdateClientConnectionSatus();
        }

        /// <summary>
        /// gets called when the mute checkbox is checked/unchecked. changes the mute status of a kinect client
        /// </summary>
        /// <param name="sender">the checkbox</param>
        /// <param name="e">unused params</param>
        private void _CheckboxMute_Checked(object sender, RoutedEventArgs e)
        {
            ConnectionStatusListviewItem obj = ((FrameworkElement)sender).DataContext as ConnectionStatusListviewItem;
            _DataManager.muteClient(obj.ID, (bool)((CheckBox)sender).IsChecked);

            if((bool)((CheckBox)sender).IsChecked){
                obj.backgroundMute = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/video_256_orange.png"));
            }
            else
            {
                obj.backgroundMute = (ImageSource)new ImageSourceConverter().ConvertFrom(new Uri(@"pack://siteoforigin:,,,/Resources/video_256_green.png"));
            }
            
        }

        /// <summary>
        /// the selection if the kinect is the master kinect for the alignment of the pictures
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CheckboxMaster_Checked(object sender, RoutedEventArgs e)
        {
            ConnectionStatusListviewItem obj = ((FrameworkElement)sender).DataContext as ConnectionStatusListviewItem;
            List<ClientConfigObject> connectedClients = _DataManager.getCurrentKinectClients();

            ClientConfigObject clientConfig = connectedClients.Find(t => t.ID == obj.ID);
            
            // resets all other kinects
            foreach (ClientConfigObject cco in connectedClients)
            {
                if (cco.ID == clientConfig.ID)
                {
                    _DataManager.masterKinect(cco, true);
                    masterSet = true;
                }
                else {
                   
                    _DataManager.masterKinect(cco, false);                    
                }
            }
            UpdateClientConnectionSatus();
        }

        /// <summary>
        /// gets called whenever a kinect gets selected in the list view and shows the current fusion perspective
        /// </summary>
        private void ConnectionStatusListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
          if(e.AddedItems.Count == 1)
          {
              try
              {
                  int currentID = ((ConnectionStatusListviewItem)e.AddedItems[0]).ID;
                  this._FusionImage.Source = this.lastFusionPictures[currentID];
                  Log.LogManager.writeLogDebug(("Client selected: " + currentID));
              }
              catch (Exception ex) { Log.LogManager.writeLogDebug(("ERROR: " + ex.Message)); }
          }
        }

        /// <summary>
        /// gets called when the user clicks the server settings button and displays a new window ClientSettings
        /// </summary>
        ClientSettingsWindow lastActiveSettingsWindow;
        private void _ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            //fetch CCO data
            ConnectionStatusListviewItem obj = ((FrameworkElement)sender).DataContext as ConnectionStatusListviewItem;
            List<ClientConfigObject> connectedClients = _DataManager.getCurrentKinectClients();

            ClientConfigObject clientConfig = connectedClients.Find(t => t.ID == obj.ID);

            //create settings window
            if (lastActiveSettingsWindow == null || !lastActiveSettingsWindow.IsLoaded)
            {
                lastActiveSettingsWindow = new ClientSettingsWindow(clientConfig);
                lastActiveSettingsWindow.OnConfigChangeEvent += lastActiveSettingsWindow_OnConfigChangeEvent;
            }
            UpdateClientConnectionSatus();            
            lastActiveSettingsWindow.Show();
        }

        /// <summary>
        /// updates a CCO object in the data base
        /// </summary>
        /// <param name="config">the CCO</param>
        private void lastActiveSettingsWindow_OnConfigChangeEvent(ClientConfigObject config)
        {
            _DataManager.UpdateClient(config);
        }

        /// <summary>
        /// starts a scan all request
        /// </summary>
        private void _ButtonScanAll_Click(object sender, RoutedEventArgs e)
        {
            if (masterSet == true)
            {                
                try
                {
                    //fetch number of pictures
                    int numberOfPictures = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.amountOfFrames;

                    //check which button has been used; true if calibration button
                    RequestType type;
                    if (sender == _ButtonCalibrateAll)
                        type = RequestType.calibration;
                    else if (sender == _ButtonScan)
                        type = RequestType.scan;
                    else
                        type = RequestType.fetch;

                    Task t = new Task(() => _DataManager.StartScanAll(numberOfPictures, type));
                    t.ContinueWith(TaskHandler, TaskContinuationOptions.OnlyOnFaulted);
                    t.Start();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
            else
            {
                MessageBox.Show("Scan not possible", "Calibration not done", MessageBoxButton.OK);
            }   
        }

        // the server SettingsWindow
        ServerSettingsWindow lastActiveServerSettingsWindow;
        private void _ButtonServerSettings_Click(object sender, RoutedEventArgs e)
        {
            //create settings window
            if (lastActiveServerSettingsWindow == null || !lastActiveServerSettingsWindow.IsLoaded)
            {
                lastActiveServerSettingsWindow = new ServerSettingsWindow();
                lastActiveServerSettingsWindow.OnConfigChangeEvent += lastActiveServerSettingsWindow_OnConfigChangeEvent;
            }
            lastActiveServerSettingsWindow.Show();
        }

        /// <summary>
        /// gets called when the server configuration has been changed
        /// </summary>
        void lastActiveServerSettingsWindow_OnConfigChangeEvent()
        {
            _DataManager.saveConfig();
        }

        /// <summary>
        /// fetches the exception from the supplied task and forwards it to the handling methods
        /// </summary>
        /// <param name="pTask">task that threw an exception</param>
        private void TaskHandler(Task pTask)
        {
            ExceptionHandler(pTask.Exception);
        }

        /// <summary>
        /// exception handler for exceptions from tasks
        /// </summary>
        private void ExceptionHandler(Exception pEx)
        {
            Log.LogManager.writeLog("ERROR: " + pEx.InnerException.Message);
        }
        /// <summary>
        /// displays the select planes window 
        /// </summary>
        SelectPlanesWindow selectPlanesWindow;
        private void _ButtonSelectPlanes_Click(object sender, RoutedEventArgs e)
        {

            //check if already loaded
            if (selectPlanesWindow != null && selectPlanesWindow.IsLoaded)
            { 
                selectPlanesWindow.Show();
                return; 
            }

            //check if reference cloud exists
            if (_DataManager.currentReferenceCloud == null) 
            { 
                MessageBox.Show("No reference cloud found.", "D'oh!", MessageBoxButton.OK); 
                return; 
            }

            //load window
            if (selectPlanesWindow != null) selectPlanesWindow.OnPlaneListUpdatedEvent -= selectPlanesWindow_OnPlaneListUpdatedEvent;
            selectPlanesWindow = new SelectPlanesWindow(_DataManager.currentReferenceCloud, Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.calibratedPlanes,this._DataManager);
            selectPlanesWindow.OnPlaneListUpdatedEvent += selectPlanesWindow_OnPlaneListUpdatedEvent;
            
            UpdateClientConnectionSatus();
            selectPlanesWindow.Show();
        }

        /// <summary>
        /// gets called when a new OnPlaneListUpdated event occurs in the SelectPlanesWindow
        /// </summary>
        /// <param name="pl">the list of selected planes</param>
        void selectPlanesWindow_OnPlaneListUpdatedEvent(List<DataIntegration.PlaneModel> pl)
        {
            this._DataManager.getNewPlaneCalibration(pl);
            UpdateClientConnectionSatus();
        }

        /// <summary>
        /// gets called when the window gets closed
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            if(this._DataManager != null) this._DataManager.RequestFusionCancellation();
        }

        /// <summary>
        /// gets called when the user clicks the euclidian scan button
        /// </summary>
        EuclidianScanWindow euclidianScanWindow;
        private void _ButtonEuclidianScan_Click(object sender, RoutedEventArgs e)
        {
            //check if already loaded
            if (euclidianScanWindow != null && euclidianScanWindow.IsLoaded)
            { euclidianScanWindow.Show(); return; }

            //check if reference cloud exists
            if (_DataManager.currentReferenceCloud == null) { MessageBox.Show("No reference cloud found.", "D'oh!", MessageBoxButton.OK); return; }

            //load window
            if (euclidianScanWindow != null) euclidianScanWindow.OnCloudListUpdatedEvent -= euclidianScanWindow_OnCloudListUpdatedEvent;
            euclidianScanWindow = new EuclidianScanWindow(_DataManager.currentReferenceCloud, Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.calibratedObjects);
            euclidianScanWindow.OnCloudListUpdatedEvent += euclidianScanWindow_OnCloudListUpdatedEvent;
            UpdateClientConnectionSatus();
            euclidianScanWindow.Show();
        }

        /// <summary>
        /// opens a container settings dialog and saves it if OK is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Button_ContainerSettings_Click(object sender, RoutedEventArgs e)
        {
            ContainerSettingsWindow containerWindow = new ContainerSettingsWindow(Config.ServerConfigManager._ServerConfigObject.calibratedContainers);
            containerWindow.ShowDialog();
            if(containerWindow.DialogResult.Value == true)
            {
                Config.ServerConfigManager._ServerConfigObject.calibratedContainers = containerWindow.containerList;
                _DataManager.saveConfig();
                writeConsole("[ContainerCalibration] New container list has been saved.");
            }
        }

        /// <summary>
        /// gets called when the euclidean scan window throws a new updated event
        /// </summary>
        /// <param name="pc">a list containing calibrated point clouds</param>
        void euclidianScanWindow_OnCloudListUpdatedEvent(List<DataIntegration.PointCloud> pc)
        {
            this._DataManager.getNewObjectCalibration(pc);
            UpdateClientConnectionSatus();
        }

        //DEBUG
        private void _ButtonDebugLoadMeshFromFileAndScan_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = "Load Mesh to scan";
            dialog.FileName = "";
            dialog.Filter = "STL Mesh Files|*.stl|All Files|*.*";

            if(true == dialog.ShowDialog())
            {
                Task t = new Task(() =>
                {
                    DataIntegration.PointCloud pcl = new DataIntegration.PointCloud(dialog.FileName);
                    pcl.downsamplePointcloud(Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.downsamplingFactor);
                    _DataManager._DataIntegrationManager_OnNewScanProcessEvent(pcl);
                });
                t.ContinueWith((continuation) => UpdateClientConnectionSatus());
                t.Start();
            }
        }

        //DEBUG
        private void _ButtonDebugLoadMeshFromFileAndSetReference_Click(object sender, RoutedEventArgs e)
        {
            this._ButtonDebugLoadMeshFromFileAndSetReference.IsEnabled = false;
            Task t = Task.Factory.StartNew(() =>
            {
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.FileName = "";
                dialog.Title = "Load Mesh/es to set as reference";
                dialog.Filter = "STL Mesh Files|*.stl|All Files|*.*";

                int loop = 0;
                while (true == dialog.ShowDialog())
                {
                    loop++;

                    if (loop == 1)
                    {
                        _DataManager.DebugSetReferenceCloud(new DataIntegration.PointCloud(dialog.FileName));
                        _DataManager.currentReferenceCloud.downsamplePointcloud(Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.downsamplingFactor);
                    }
                    else
                    {
                        DataIntegration.PointCloud pcl = new DataIntegration.PointCloud(dialog.FileName);
                        pcl.downsamplePointcloud(Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.downsamplingFactor);                        
                        double[,] transformationMatrix = new double[4,4];
                        this.Dispatcher.Invoke(() =>
                        {
                            DebugTransformationMatrix traWin = new DebugTransformationMatrix();
                            traWin.ShowDialog();
                            transformationMatrix = traWin.transformationMatrix;
                        });
                        _DataManager.currentReferenceCloud.addPointcloudToReference(pcl,
                            transformationMatrix,
                            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.ICP_perform,
                            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.ICP_indis);
                    }
                }
            });

            t.ContinueWith((continuation) =>
                {
                    this.Dispatcher.Invoke(() =>
                        {
                            UpdateClientConnectionSatus();
                            this._DataManager_OnNewPointPicturesEvent(_DataManager.currentReferenceCloud.pictures);
                            this._ButtonSelectPlanes.IsEnabled = true;
                            this._ButtonEuclidianScan.IsEnabled = true;
                            this._ButtonDebugLoadMeshFromFileAndSetReference.IsEnabled = true;
                        });
                });
        }

    }
}
