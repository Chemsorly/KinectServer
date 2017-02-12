using Post_knv_Server.DataIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post_knv_Server.CalculationService
{
    /// <summary>
    /// holding class for intermediate scan results during the scan process
    /// </summary>
    class IntermediateScanResultPackage
    {
        /// <summary>
        /// the point cloud
        /// </summary>
        public PointCloud pointCloud { get; set; }

        /// <summary>
        /// average height for the point cloud
        /// </summary>
        public double averageHeight { get; set; }

        /// <summary>
        /// calculated convex area
        /// </summary>
        public double convexPlaneArea { get; set; }

        /// <summary>
        /// calculated concav area
        /// </summary>
        public double concavPlaneArea { get; set; }
    }
}
