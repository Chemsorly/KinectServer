using Post_KNV_MessageClasses;
using RequestType = Post_KNV_MessageClasses.ClientConfigObject.RequestType;
using Post_knv_Server.DataIntegration;
using Post_knv_Server.Webservice;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Post_knv_Server.DataManager
{
        /// <summary>
        /// this class represents the DataManager
        /// </summary>
        public class DataManager
        {
            #region fields

            //cancellation source
            System.Threading.CancellationTokenSource _CancelTokenSource;

            //singleton instance of the data manager
            private static DataManager _DataManager;
            public static DataManager getInstance()
            {
                if (_DataManager == null)
                    _DataManager = new DataManager();
                return _DataManager;
            }

            //reference cloud
            public PointCloud currentReferenceCloud { get { return this._DataIntegrationManager.currentReferenceCloud; } }

            //webservice reference
            private Webservice.ServerHandler _Webservice;

            //database manager
            private DBManager _DBManager;

            //configManager for server config
            Config.ServerConfigManager _ConfigManager;

            //data integration manager
            DataIntegration.DataIntegrationManager _DataIntegrationManager;

            //calculation service
            CalculationService.CalculationManager _CalculationManager;

            #endregion

            /// <summary>
            /// initializes the dataManager
            /// </summary>
            public void initialize()
            {
                _CancelTokenSource = new System.Threading.CancellationTokenSource();
                _ConfigManager = new Config.ServerConfigManager();
                _DBManager = new DBManager();

                if (_Webservice != null) _Webservice.killWebservice();

                _Webservice = new Webservice.ServerHandler(_DBManager);

                _DataIntegrationManager = new DataIntegration.DataIntegrationManager();
                _DataIntegrationManager.OnNewFusionPictureEvent += _DataIntegrationManager_OnNewFusionPictureEvent;
                _DataIntegrationManager.OnNewPointPicturesEvent += _DataIntegrationManager_OnNewPointPicturesEvent;
                _DataIntegrationManager.OnNewScanProcessEvent += _DataIntegrationManager_OnNewScanProcessEvent;

                try
                {
                    _DataIntegrationManager.setReferenceCloud(PointCloud.importPointCloud(@"config\referenceCloud.dat"));
                    this._DataIntegrationManager_OnNewPointPicturesEvent(_DataIntegrationManager.currentReferenceCloud.pictures);
                }
                catch (Exception ex) { Log.LogManager.writeLog("ERROR: " + ex.Message); }

                _CalculationManager = new CalculationService.CalculationManager();
                _CalculationManager.OnResultPackageEvent += _CalculationManager_OnResultPackageEvent;
                _CalculationManager.OnNewPointPicturesEvent += _CalculationManager_OnNewPointPicturesEvent;

                // subscribe on the server handler kinect data event
                _Webservice.OnKinectDataPackageSendEvent += _Webservice_OnKinectDataPackageSendEvent;
                _Webservice.OnAppScanrequestEvent += _Webservice_OnAppScanrequestEvent;
                
            }

            
            #region external

            //event for point pictures forwarding
            internal delegate void OnNewPointPicturesReady(DataIntegration.PointCloudDrawing.PointCloudPicturePackage pointPictures);
            internal event OnNewPointPicturesReady OnNewPointPicturesEvent;

            //event for fusion picture forwarding
            internal delegate void OnNewFusionPictureReady(int ID, WriteableBitmap fusionPicture);
            internal event OnNewFusionPictureReady OnNewFusionPictureEvent;

            //event for new scan results ready
            internal delegate void OnNewScanResultsReady(ScanResultPackage pPackage);
            internal event OnNewScanResultsReady OnNewScanResultEvent;

            /// <summary>
            /// gets a new plane calibration, sets it in the settings, removes the planes from the current point cloud and updates the pictures
            /// </summary>
            /// <param name="pPlaneModels">the planes</param>
            public void getNewPlaneCalibration(List<PlaneModel> pPlaneModels)
            {
                Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.calibratedPlanes = pPlaneModels;
                _ConfigManager.writeConfig();
                _DataIntegrationManager.currentReferenceCloud.removePlaneFromPointcloud(pPlaneModels, Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.planar_ThresholdDistance);
                this._DataIntegrationManager_OnNewPointPicturesEvent(_DataIntegrationManager.currentReferenceCloud.pictures);
            }

            /// <summary>
            /// gets a new object calibration, sets it in the settings, removes the objects from the current point cloud and updates the pictures
            /// </summary>
            /// <param name="pObjects">the list of objects</param>
            public void getNewObjectCalibration(List<PointCloud> pObjects)
            {
                Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.calibratedObjects = pObjects;
                _ConfigManager.writeConfig();
                int rp = _DataIntegrationManager.currentReferenceCloud.removePointcloudsFromPointCloud(pObjects, Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.euclidean_ExtractionRadius);
                Log.LogManager.writeLog("[DataManager] " + rp + " points removed from reference cloud.");
                this._DataIntegrationManager_OnNewPointPicturesEvent(_DataIntegrationManager.currentReferenceCloud.pictures);
            }

            /// <summary>
            /// fetch current clients from the database
            /// </summary>
            /// <returns>returns a lit of CCOs</returns>
            public List<ClientConfigObject> getCurrentKinectClients()
            {
                return _DBManager.recieveClientList();
            }

            /// <summary>
            /// delete selected client from the database
            /// </summary>
            /// <param name="pID">the ID of the client</param>
            public void deleteClientFromDatabase(int pID)
            {
                _DBManager.deleteKinectFromDatabase(pID);
            }

            /// <summary>
            /// shutdown selected client
            /// </summary>
            /// <param name="pID">the ID of the client</param>
            public void shutdownClient(int pID)
            {
                ClientConfigObject cco = _DBManager.getCcoByID(pID);

                if (cco.clientRequestObject.isConnected == true)
                {
                    cco.clientRequestObject.isConnected = false;
                    Log.LogManager.writeLog("[DataManager:DataManager] Client " + cco.name + " has been shutdown.");
                }
                else
                {
                    cco.clientRequestObject.isConnected = true;
                    Log.LogManager.writeLog("[DataManager:DataManager] Attempting to start Client " + cco.name + ".");
                }

                _Webservice.sendShutdownToClient(cco);
                _DBManager.updateClient(cco, false);
            }

            /// <summary>
            /// mute the selected client
            /// </summary>
            /// <param name="pID">the ID of the client</param>
            /// <param name="mute">the new mute status</param>
            public void muteClient(int pID, bool mute)
            {
                ClientConfigObject cco = _DBManager.getCcoByID(pID);
                cco.clientRequestObject.isMuted = mute;

                if (cco.clientRequestObject.isMuted) Log.LogManager.writeLog("[DataManager:DataManager] Client " + cco.name + " has been muted.");
                else Log.LogManager.writeLog("[DataManager:DataManager] Client " + cco.name + " has been unmuted.");

                _DBManager.updateClient(cco, true);
            }

            /// <summary>
            /// updates the kinect status if the kinect is selected or deselected as master kinect
            /// </summary>
            /// <param name="pID"></param>
            /// <param name="master"></param>
            public void masterKinect(ClientConfigObject pCco, bool master)
            {                
               pCco.clientKinectConfig.isMaster = master;
               if (pCco.clientKinectConfig.isMaster) Log.LogManager.writeLog("[DataManager:DataManager] Client " + pCco.name + " is now the master Kinect.");           

               _DBManager.updateClient(pCco, true);
            }


            /// <summary>
            /// update client data
            /// </summary>
            /// <param name="config">the CCO object of the client</param>
            public void UpdateClient(ClientConfigObject config)
            {
                _DBManager.updateClient(config, false);
            }


            /// <summary>
            /// scan all clients for kinect data
            /// </summary>
            public String StartScanAll(int pNumberOfPictures, RequestType pScantype)
            {
                List<ClientConfigObject> clientList = _DBManager.recieveClientList();

                //master check
                int masters = 0;
                foreach (ClientConfigObject cco in clientList)
                    if (cco.clientKinectConfig.isMaster && !cco.clientRequestObject.isMuted && cco.clientRequestObject.isConnected)
                        masters++;
                if (masters != 1)
                    throw new Exception("Amount of active master Kinects doesnt equal 1. Amount: " + masters);

                //start
                _DataIntegrationManager.resetDataIntegration();

                int counter = clientList.Where(t => t.clientRequestObject.isConnected && !t.clientRequestObject.isMuted).Count();
                foreach (ClientConfigObject cco in clientList)
                {
                    if (cco.clientRequestObject.isConnected && !cco.clientRequestObject.isMuted)
                    {
                        cco.clientRequestObject.requestType = pScantype;
                        cco.clientKinectConfig.numberOfPictures = pNumberOfPictures;
                        cco.clientRequestObject.amountOfKinectsInRequest = counter;
                        _Webservice.sendScanRequestToClient(cco);
                    }
                }
               
                //output
                if (pScantype == RequestType.calibration)
                {   
                    Log.LogManager.writeLog("[DataManager:DataManager] CalibrationRequest sent to " + counter + " clients.");
                    Log.LogManager.updateAlgorithmStatus("Scan started");
                    return "OK";
                }
                else if (pScantype == RequestType.fetch)
                {   
                    Log.LogManager.writeLog("[DataManager:DataManager] DataFetchRequest sent to " + counter + " clients.");
                    Log.LogManager.updateAlgorithmStatus("Scan started");
                    return "OK";
                }
                else if (pScantype == RequestType.scan)
                {
                    Log.LogManager.writeLog("[DataManager:DataManager] ScanRequest sent to " + counter + " clients.");
                    Log.LogManager.updateAlgorithmStatus("Scan started");
                    return "OK";
                }
                return String.Empty;
            }

            /// <summary>
            /// saves the new ServerConfigObject
            /// </summary>
            /// <param name="pSCO">the SCO</param>
            public void saveConfig()
            {
                _ConfigManager.writeConfig();
            }

            /// <summary>
            /// returns a list of Planes based on provided settings in the SCO
            /// </summary>
            /// <param name="amount">the amount of planes</param>
            /// <returns>a list of planes</returns>
            public List<PlaneModel> getPlanes(int amount, System.Threading.CancellationToken pTaskToken)
            {
                return _DataIntegrationManager.getPlanes(amount, Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.planar_Iterations,
                    Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.planar_ThresholdDistance,
                    Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.planar_planeComparisonVarianceThreshold,
                    pTaskToken);
            }
            
            /// <summary>
            /// requests cancellation for kinect fusion via CancellationToken
            /// </summary>
            public void RequestFusionCancellation()
            {
                this._CancelTokenSource.Cancel();
                this._CancelTokenSource = new System.Threading.CancellationTokenSource();
            }

            #endregion

            #region internal

            /// <summary>
            /// gets thrown when a new scan result is reported, ready from the calculation service
            /// </summary>
            /// <param name="pResult">the result package</param>
            void _CalculationManager_OnResultPackageEvent(ScanResultPackage pResult)
            {
                _Webservice.sendScanResults(pResult);
                OnNewScanResultEvent(pResult);
            }

            /// <summary>
            /// gets fired whenever a point picture package is ready; forwards it by throwing a new event
            /// </summary>
            /// <param name="pointPictures">the point pictures package</param>
            void _DataIntegrationManager_OnNewPointPicturesEvent(DataIntegration.PointCloudDrawing.PointCloudPicturePackage pointPictures)
            {
                OnNewPointPicturesEvent(pointPictures);
                _DataIntegrationManager.currentReferenceCloud.exportPointCloud();
            }
            
            /// <summary>
            /// gets fired whenever a fusion picture is ready; forwards it by throwing a new event
            /// </summary>
            /// <param name="fusionPicture">the fusion picture</param>
            void _DataIntegrationManager_OnNewFusionPictureEvent(int pID, System.Windows.Media.Imaging.WriteableBitmap fusionPicture)
            {
                OnNewFusionPictureEvent(pID,fusionPicture);
            }

            /// <summary>
            /// recieves the KinectDataPackage by adding it to the Database and creating a point cloud from it
            /// </summary>
            /// <param name="input"></param>
            private void _Webservice_OnKinectDataPackageSendEvent(KinectDataPackage input)
            {
                this._DataIntegrationManager.recieveDataPackage(input, this._CancelTokenSource.Token);               
            }

            /// <summary>
            /// gets a scan request via app or website, tries to start the scan and returns a status string
            /// </summary>
            /// <param name="EventArgs">the app request package</param>
            /// <returns>if request successful or not</returns>
            string _Webservice_OnAppScanrequestEvent(AppRequestObject EventArgs)
            {
                try
                {
                    return this.StartScanAll(Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.amountOfFrames, RequestType.scan);
                }
                catch (Exception ex) { return ex.Message; }
            }

            /// <summary>
            /// gets fired when the calculation servce broadcasts a new picture
            /// </summary>
            /// <param name="pointPictures">the pictures</param>
            void _CalculationManager_OnNewPointPicturesEvent(PointCloudDrawing.PointCloudPicturePackage pointPictures)
            {
                this.OnNewPointPicturesEvent(pointPictures);
            }

            /// <summary>
            /// gets fired when all kinects for the scan process have been processed
            /// </summary>
            /// <param name="pcl">the current reference cloud</param>
            public void _DataIntegrationManager_OnNewScanProcessEvent(PointCloud pcl)
            {
                this._CalculationManager.calculateResults(pcl,
                    Config.ServerConfigManager._ServerConfigObject);
            }      

            #endregion

            #region debug

            public void DebugSetReferenceCloud(PointCloud inputCloud)
            {                
                this._DataIntegrationManager.setReferenceCloud(inputCloud);
                Log.LogManager.writeLogDebug("[DEBUG] Reference cloud set");
            }

            #endregion
        }
   
}
