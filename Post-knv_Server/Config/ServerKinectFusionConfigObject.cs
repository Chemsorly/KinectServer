using Microsoft.Kinect.Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Post_knv_Server.Config
{
    /// <summary>
    /// object that contains the configuration parameters for kinect fusion
    /// </summary>
    [Serializable()]
    public class ServerKinectFusionConfigObject
    {
        public ReconstructionProcessor ProcessorType { get; set; }          //the reconstruction processor type. GPU (AMP) or CPU
        public int DeviceToUse { get; set; }                                //the device to use
        public bool AutoResetReconstructionWhenLost { get; set; }           //reset reconstruction when the Kinect Fusion algorithm loses tracking
        public bool translateResetPoseByMinDepthThreshold { get; set; }
        public int MaxTrackingErrors { get; set; }                          //maximum tracking errors until reset
        public int depthHeight { get; set; }                                //height of the depth picture
        public int depthWidth { get; set; }                                 //width of the depth picture
        public float minDepthClip { get; set; }                             //minimum depth distance
        public float maxDepthClip { get; set; }                             //maximum depth distance
        public int VoxelsPerMeter { get; set; }                             //the voxels per meter resolution in the kinect fusion volume
        public int VoxelResolutionX { get; set; }                           //the x-resolution in kinfu volume
        public int VoxelResolutionY { get; set; }                           //the y-resolution in kinfu volume
        public int VoxelResolutionZ { get; set; }                           //the z-resolution in kinfu volume
        public int integrationWeight { get; set; }                          //kinect fusion value
        public int iterationCount { get; set; }                             //kinect fusion value
    }
}
