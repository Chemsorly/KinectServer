using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post_KNV_MessageClasses
{
    [Serializable()]
    public class ClientKinectConfigObject
    {
        /// <summary>
        /// the amount of pictures used for a scan request
        /// </summary>
        public int numberOfPictures { get; set; }

        /// <summary>
        /// determines if the color picture is captured or not
        /// </summary>
        public bool calculateColor { get; set; }

        /// <summary>
        /// the minimum depth to be captured by the kinect
        /// </summary>
        public ushort minDepth { get; set; }

        /// <summary>
        /// the maximum depth to be captured by the kinect
        /// </summary>
        public ushort maxDepth { get; set; }

        /// <summary>
        /// the minimum x Value for the depth picture to be captured
        /// </summary>
        public int xMinDepth { get; set; }

        /// <summary>
        /// the maximum x Value for the depth picture to be captured
        /// </summary>
        public int xMaxDepth { get; set; }

        /// <summary>
        /// the minimum y Value for the depth picture to be captured
        /// </summary>
        public int yMinDepth { get; set; }

        /// <summary>
        /// the maximum y Value for the depth picture to be captured
        /// </summary>
        public int yMaxDepth { get; set; }

        /// <summary>
        /// the Kinect is the configured master in the setup
        /// </summary>
        public bool isMaster { get; set; }

        /// <summary>
        /// transformation matrix for point cloud alignment
        /// </summary>
        public double[,] transformationMatrix { get; set; }
    }
}
