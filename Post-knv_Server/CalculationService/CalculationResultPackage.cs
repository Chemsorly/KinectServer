using System;
using Algorithms = Post_knv_Server.CalculationService.CalculationManager.Algorithms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post_knv_Server.CalculationService
{
    /// <summary>
    /// a data package containing results and metadata from performed algorithms
    /// </summary>
    public class CalculationResultPackage
    {
        //content to change

        //the algorithm used
        public Algorithms algorithmUsed { get; set; }

        //the scanned volume of the payload of the containers: Priority low
        public float scannedDelaunayVolume { get; set; }

        public float scannedPlanarVolume { get; set; }

        //the amount of containers in the area: Priority high
        public int numberOfContainers { get; set; }

        //the amount of points in the point cloud: Priority low
        public int numberOfPoints { get; set; }

    }
}
