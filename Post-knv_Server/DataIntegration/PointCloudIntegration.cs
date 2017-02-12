using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using icp_net;
using Post_knv_Server.DataIntegration;
using Point = Post_knv_Server.DataIntegration.PointCloud.Point;
using Vector3 = Microsoft.Kinect.Fusion.Vector3;

namespace Post_knv_Server.DataIntegration
{
    /// <summary>
    /// class to start the point cloud integration algorithm
    /// </summary>
    public class PointCloudIntegration
    {
        /// <summary>
        /// integrates two point clouds with each other; the adding cloud gets integrated into the reference cloud
        /// </summary>
        /// <param name="referencePointCloud">the reference cloud</param>
        /// <param name="addingPointCloud">the adding cloud</param>
        public static void integratePointClouds(PointCloud referencePointCloud, PointCloud addingPointCloud, double[,] pTransformationMatrix, bool pUseICP, int inlierDistance)
        {
            //align point clouds
            double[,] newPoints = Algorithm.PointCloudAlignment.alignPointClouds(referencePointCloud, addingPointCloud, pTransformationMatrix, pUseICP, inlierDistance);

            //add new points to point cloud
            for (int i = 0; i < addingPointCloud.count; i++)
                referencePointCloud.pointcloud_hs.Add(new Point(new Vector3() { X = (float)newPoints[i, 0], Y = (float)newPoints[i, 1], Z = (float)newPoints[i, 2] }));  
        }

    }
}
