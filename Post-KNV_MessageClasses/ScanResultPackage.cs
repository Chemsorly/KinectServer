using Post_knv_Server.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post_KNV_MessageClasses
{
    /// <summary>
    /// a data package containing results and metadata from performed algorithms
    /// </summary>
    [Serializable()]
    public class ScanResultPackage
    {
        /// <summary>
        /// the amount of kinects data was requested from
        /// </summary>
        public int amountOfKinectsDataRequested { get; set; }

        /// <summary>
        /// timestamp of the result
        /// </summary>
        public DateTime timestamp { get; set; }

        /// <summary>
        /// the algorithm used
        /// </summary>
        public Algorithms algorithmUsed { get; set; }

        /// <summary>
        /// the scanned volume of the payload of the containers
        /// </summary>
        public double scannedDelaunayVolume { get; set; }
        public double convexScannedAlgorithmVolume { get; set; }
        public double concavScannedAlgorithmVolume { get; set; }
        
        /// <summary>
        /// the amount of containers in the area
        /// </summary>
        public int numberOfClouds { get; set; }

        /// <summary>
        /// the amount of points in the point cloud
        /// </summary>
        public int numberOfPoints { get; set; }

        /// <summary>
        /// estimated accuracy of the container guess
        /// </summary>
        public double containerAccuracy { get; set; }

        /// <summary>
        /// the amount of guessed containers
        /// </summary>
        public List<ContainerResult> containerResults { get; set; }

        /// <summary>
        /// the amount of guessed payload
        /// </summary>
        public double estimatedPayloadVolume { get; set; }
            
        /// <summary>
        /// holder class for the container type
        /// </summary>
        public class ContainerResult
        {
            /// <summary>
            /// amount of containers
            /// </summary>
            public int amount { get; set; }

            /// <summary>
            /// container type
            /// </summary>
            public ContainerConfigObject containerType { get; set; }
        }        
    }

    /// <summary>
    /// the algorithms used by the application
    /// </summary>
    public enum Algorithms { ConvexConcavEnsembled};

}
