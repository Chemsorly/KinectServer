using ANX.Framework;
using Post_KNV_MessageClasses;
using Post_knv_Server.DataIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post_knv_Server.Config
{
    /// <summary>
    /// this class represents a server algorithm object
    /// </summary>
    [Serializable()]
    public class ServerAlgorithmConfigObject
    {
        public float downsamplingFactor { get; set; }                        //downsampling factor for point cloud downsampling
        public int amountOfFrames { get; set; }                              //amount of frames captured from the kinects
        public int ICP_indis { get; set; }                                   //inlier distance for ICP
        public bool ICP_perform { get; set; }                                //perform ICP?

        public float euclidean_ExtractionRadius { get; set; }                //extraction radius for the euclidean cluster extraction
        public float euclidean_MinimumVolume { get; set; }                   //the minimum volume a point cloud has to have, otherwise it gets discarded

        public int planar_Iterations { get; set; }                           //the amount of iterations per plane for the planar model segmentation
        public float planar_ThresholdDistance { get; set; }                  //the threshold distance for inliers for the planar model segmentation
        public float planar_planeComparisonVarianceThreshold { get; set; }   //a plane comparison value to distinguish between similar planes

        public float concav_angleThreshold { get; set; }                     //angle threshold used during the concav hull algorithm

        public Algorithms useAlgorithm { get; set; }                         //the algorithm to use in the calculation service
        public float correctionValue { get; set; }                           //a value to correct the inaccuracy of the scanning process

        public List<PlaneModel> calibratedPlanes { get; set; }               //a list of the calibrated planes that are supposed to be excluded from scans
        public List<PointCloud> calibratedObjects { get; set; }              //a list of objects that are supposed to be excluded from future scans
    }
}
