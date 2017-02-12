using icp_net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Post_knv_Server.DataIntegration
{
    class DataIntegrationManager
    {
        #region fields

        /// <summary>
        /// the reconstruction manager
        /// </summary>
        DataIntegrationReconstruction _ReconstructionVolume;

        /// <summary>
        /// the current reference cloud
        /// </summary>
        public PointCloud currentReferenceCloud { get; private set; }

        /// <summary>
        /// incoming clouds the program will work on
        /// </summary>
        List<PointCloud> incomingCloudList;

        /// <summary>
        /// is a master cloud recieved?
        /// </summary>
        bool masterRecieved;

        /// <summary>
        /// lock object to ensure not multiple processes are working
        /// </summary>
        Object isWorking = new Object(); 

        /// <summary>
        /// the amount of integrated clouds in current request
        /// </summary>
        int integratedClouds = 0;

        #endregion

        #region external

        //event for fusion picture forwarding
        internal delegate void OnNewFusionPictureReady(int ID, WriteableBitmap fusionPicture);
        internal event OnNewFusionPictureReady OnNewFusionPictureEvent;

        //event for point pictures forwarding
        internal delegate void OnNewPointPicturesReady(PointCloudDrawing.PointCloudPicturePackage pointPictures);
        internal event OnNewPointPicturesReady OnNewPointPicturesEvent;

        //event for calibration service processing
        internal delegate void OnNewScanProcessReady(PointCloud pcl);
        internal event OnNewScanProcessReady OnNewScanProcessEvent;

        /// <summary>
        /// constructor
        /// </summary>
        internal DataIntegrationManager()
        {
            incomingCloudList = new List<PointCloud>();
            masterRecieved = false;
            this._ReconstructionVolume = new DataIntegrationReconstruction();
            this._ReconstructionVolume.OnNewFusionPictureEvent += _ReconstructionVolume_OnNewFusionPictureEvent;
            this._ReconstructionVolume.OnNewPointCloudEvent += _ReconstructionVolume_OnNewPointCloudEvent;
        }

        /// <summary>
        /// sets the input point cloud as reference
        /// </summary>
        /// <param name="pPointCloud">the input point cloud</param>
        internal void setReferenceCloud(PointCloud pPointCloud)
        {
            this.currentReferenceCloud = pPointCloud;
        }
        
        /// <summary>
        /// recieves a KinectDataPackage and creates a PointCloud from it; as soon the PointCloud is ready, the ReconstructionVolume throws an event
        /// </summary>
        /// <param name="pDataPackage">the data package</param>
        internal void recieveDataPackage(Post_KNV_MessageClasses.KinectDataPackage pDataPackage, System.Threading.CancellationToken pCancelToken)
        {
            Log.LogManager.writeLog("[DataIntegrationManager] Kinect Data Package received from Client " + pDataPackage.usedConfig.name);
            this._ReconstructionVolume.acceptKinectDataPackage(pDataPackage, pCancelToken);
        }

        /// <summary>
        /// creates a list of planes based on the current reference cloud
        /// </summary>
        /// <param name="pAmount">the amount of planes</param>
        /// <param name="pIterations">the amount of iterations for each plane</param>
        /// <param name="pPlaneDistanceThreshold">the distance theshold for the inliers</param>
        /// <returns>a list of planes</returns>
        internal List<PlaneModel> getPlanes(int pAmount, int pIterations, float pPlaneDistanceThreshold, float pPlaneVarianceThreshold, System.Threading.CancellationToken pTaskToken)
        {
            return Algorithm.PlanarModelSegmentation.doPlanarModelIterative(currentReferenceCloud, pIterations, pPlaneDistanceThreshold, pAmount, pPlaneVarianceThreshold, pTaskToken);
        }

        /// <summary>
        /// sets the current reference cloud to null
        /// </summary>
        internal void resetDataIntegration()
        {
            //check if another thread is already working on the integration
            if (!System.Threading.Monitor.TryEnter(isWorking))
                throw new Exception("Dataintegration is busy.");
            System.Threading.Monitor.Exit(isWorking);

            //check if another thread is already working on reconstruction
            if (!System.Threading.Monitor.TryEnter(this._ReconstructionVolume.canWorkLock))
                throw new Exception("Reconstruction is busy.");
            System.Threading.Monitor.Exit(this._ReconstructionVolume.canWorkLock);

            this.currentReferenceCloud = null;
            incomingCloudList.Clear();
            masterRecieved = false;
            integratedClouds = 0;
            Log.LogManager.writeLog("[DataIntegrationManager] Current reference cloud deleted");
        }

        #endregion

        #region internal

        /// <summary>
        /// forwards the FusionPicture by throwing an event
        /// </summary>
        /// <param name="fusionPicture">the fusion picture</param>
        void _ReconstructionVolume_OnNewFusionPictureEvent(int pID, System.Windows.Media.Imaging.WriteableBitmap fusionPicture)
        {
            OnNewFusionPictureEvent(pID, fusionPicture);
        }
        
        /// <summary>
        /// adds a new point cloud to the current reference cloud
        /// </summary>
        /// <param name="pcl">the new point cloud that is to be added to the reference cloud</param>
        void _ReconstructionVolume_OnNewPointCloudEvent(PointCloud pcl)
        {
            //check if master
            if (pcl.ConfigObject.clientKinectConfig.isMaster)
                masterRecieved = pcl.ConfigObject.clientKinectConfig.isMaster;

            incomingCloudList.Add(pcl);

            if (masterRecieved)
                workPointcloudQueue();
        }

        /// <summary>
        /// works through the current point cloud queue
        /// </summary>
        void workPointcloudQueue()
        {            
            while (incomingCloudList.Count > 0)
            {
                //if master exists. If yes, take it, if not take first in line
                PointCloud pcl;

                if (incomingCloudList.Exists(t => t.ConfigObject.clientKinectConfig.isMaster))
                    pcl = incomingCloudList.Find(t => t.ConfigObject.clientKinectConfig.isMaster);
                else
                    pcl = incomingCloudList.First();
                incomingCloudList.Remove(pcl);
                
                //downsample point cloud for performance
                if (Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.downsamplingFactor > 0)
                    pcl.downsamplePointcloud(Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.downsamplingFactor);

                lock (isWorking)
                {
                    integratedClouds++;

                    //check if reference cloud is set, if not, do and return
                    if (currentReferenceCloud == null)
                    {
                        currentReferenceCloud = pcl;
                        OnNewPointPicturesEvent(currentReferenceCloud.pictures);
                        Log.LogManager.writeLog("[DataIntegration] Reference cloud set: Amount of points: " + currentReferenceCloud.count);
                    }
                    else
                    {
                        //adds new pointcloud to reference
                        currentReferenceCloud.addPointcloudToReference(pcl, pcl.ConfigObject.clientKinectConfig.transformationMatrix,
                            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.ICP_perform,
                            Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.ICP_indis);
                        Log.LogManager.writeLog("[DataIntegration] Pointclouds aligned. Amount of points: " + currentReferenceCloud.count);

                        //broadcast
                        OnNewPointPicturesEvent(currentReferenceCloud.pictures);
                        Log.LogManager.updateAlgorithmStatus("Done");
                        Log.LogManager.writeLog("[DataIntegration] DataIntegration process done with " + integratedClouds + " point clouds.");
                    }

                    //if request was a scan request and all point clouds have been integrated, throw new OnNewScanProcess event to move data to calculation service
                    if(currentReferenceCloud.ConfigObject.clientRequestObject.requestType == Post_KNV_MessageClasses.ClientConfigObject.RequestType.scan && 
                        integratedClouds == currentReferenceCloud.ConfigObject.clientRequestObject.amountOfKinectsInRequest)
                    {
                        OnNewScanProcessEvent(this.currentReferenceCloud);
                    }
                }
            }            
        }

        #endregion
    }
}
