using Microsoft.Kinect.Fusion;
using Post_KNV_MessageClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Post_knv_Server.Config
{
    /// <summary>
    /// this class represents the server config objects
    /// </summary>
    [Serializable()]
    public class ServerConfigObject
    {
        #region variables & fields

        /// <summary>
        /// the server IP
        /// </summary>
        public string ownIP { get; set; }

        /// <summary>
        /// the address if the gateway (optional)
        /// </summary>
        public string gatewayAddress { get; set; }

        /// <summary>
        /// the address for the SignalR server to broadcast results to mobile applications (optional)
        /// </summary>
        public string signalrAddress { get; set; }

        /// <summary>
        /// interval to send out PINGs to check for client connectivity
        /// </summary>
        public double keepAliveInterval { get; set; }

        /// <summary>
        /// port to listen on
        /// </summary>
        public int listeningPort { get; set; }

        /// <summary>
        /// enable or disable debug messages
        /// </summary>
        public bool debug { get; set; }

        /// <summary>
        /// algorithm config object
        /// </summary>
        public ServerAlgorithmConfigObject serverAlgorithmConfig { get; set; }

        /// <summary>
        /// kinect fusion config object
        /// </summary>
        public ServerKinectFusionConfigObject serverKinectFusionConfig { get; set; }

        /// <summary>
        /// a list of calibrated/saved containers
        /// </summary>
        public List<ContainerConfigObject> calibratedContainers { get; set; }

        #endregion


        /// <summary>
        /// load a default config for the client
        /// </summary>
        /// <returns>a default config object</returns>
        public static ServerConfigObject GetDefaultConfig()
        {
            return new ServerConfigObject()
            {
                ownIP = ClientConfigObject.getOwnIP(),
                gatewayAddress = String.Empty,
                signalrAddress = String.Empty,
                keepAliveInterval = 5000,
                listeningPort = 8999,
                debug = true,
                serverAlgorithmConfig = new ServerAlgorithmConfigObject()
                {
                    downsamplingFactor = 0.005f,
                    amountOfFrames = 10,
                    ICP_perform = false,
                    ICP_indis = -1,
                    euclidean_ExtractionRadius = 0.1f,
                    euclidean_MinimumVolume = 1,
                    planar_Iterations = 10000,
                    planar_ThresholdDistance = 0.05f,
                    useAlgorithm = Algorithms.ConvexConcavEnsembled,
                    calibratedPlanes = new List<DataIntegration.PlaneModel>(),
                    calibratedObjects = new List<DataIntegration.PointCloud>(),
                    planar_planeComparisonVarianceThreshold = 0.1f,
                    concav_angleThreshold = 90,
                    correctionValue = 0
                },
                serverKinectFusionConfig = new ServerKinectFusionConfigObject()
                {
                    ProcessorType = Microsoft.Kinect.Fusion.ReconstructionProcessor.Cpu,
                    DeviceToUse = -1,
                    AutoResetReconstructionWhenLost = true,
                    MaxTrackingErrors = 100,
                    depthHeight = 424,
                    depthWidth = 512,
                    minDepthClip = FusionDepthProcessor.DefaultMinimumDepth,
                    maxDepthClip = FusionDepthProcessor.DefaultMaximumDepth,
                    translateResetPoseByMinDepthThreshold = true,
                    VoxelsPerMeter = 32,
                    VoxelResolutionX = 256,
                    VoxelResolutionY = 256, 
                    VoxelResolutionZ = 256,
                    integrationWeight = FusionDepthProcessor.DefaultIntegrationWeight,
                    iterationCount = FusionDepthProcessor.DefaultAlignIterationCount
                }, calibratedContainers = new List<ContainerConfigObject>()
            };
        }
    }
}
