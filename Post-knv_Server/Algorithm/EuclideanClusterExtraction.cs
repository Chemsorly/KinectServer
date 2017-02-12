using KDTree;
using Post_knv_Server.Config;
using Post_knv_Server.DataIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Point = Post_knv_Server.DataIntegration.PointCloud.Point;

namespace Post_knv_Server.Algorithm
{
    /// <summary>
    /// class that contains methods to perform a EuclideanClusterExtraction on a PointCloud
    /// </summary>
    static class EuclideanClusterExtraction
    {
        /// <summary>
        /// calculates an euclidean cluster extraction from the point cloud based on a euclidean distance. returns a list of point clouds
        /// </summary>
        /// <param name="pInputCloud">the combined point cloud</param>
        /// <param name="pConfig">the config object for the algorithms</param>
        /// <returns>a list of point clouds</returns>
        public static List<PointCloud> calculateEuclideanClusterExtraction(PointCloud pInputCloud, float pEuclideanExtractionRadius, CancellationToken pToken)
        {
            //update status
            Log.LogManager.updateAlgorithmStatus("Euclidean Cluster Extraction");
            Log.LogManager.writeLogDebug("[EuclideanClusterExtraction] Extraction Radius: " + pEuclideanExtractionRadius);

            //creates the end list to be returned
            List<PointCloud> clusters = new List<PointCloud>();
            foreach (Point p in pInputCloud.pointcloud_hs)
            {
                pToken.ThrowIfCancellationRequested();

                //checks if point has been processed already, if yes, skip
                if (p.processed) continue;

                //create new queue
                Queue<Point> Q = new Queue<Point>();

                //add point to queue
                Q.Enqueue(p);

                //result values
                HashSet<Point> resultCloud = new HashSet<Point>();
                PointCloud resultCluster = new PointCloud(resultCloud);

                //while queue has points, check for neighbours and add them to queue as well, do as long there are neighbours
                while (Q.Count > 0)
                {
                    pToken.ThrowIfCancellationRequested();

                    //remove point from queue and add to current cluster, point has been processed by doing that
                    Point p2 = Q.Dequeue();
                    if (p2.processed) continue;
                    resultCloud.Add(p2);
                    p2.processed = true;

                    //check all neighbour points, add them to queue if they havnt been processed yet
                    double[] tp = { p2.point.X, p2.point.Y, p2.point.Z };
                    NearestNeighbour<Point> nb = lookForNeighbours(pInputCloud.pointcloud_kd, tp, pEuclideanExtractionRadius);
                    while (nb.MoveNext())
                    {
                        if (!nb.Current.processed) Q.Enqueue(nb.Current);
                    }
                }

                clusters.Add(resultCluster);
            }

            //set all points as unprocessed, so they can be used for other algorithms
            Parallel.ForEach(clusters, pointcloud => Parallel.ForEach(pointcloud.pointcloud_hs, point => point.processed = false));

            //updates the status
            Log.LogManager.updateAlgorithmStatus("Done");

            //return
            return clusters;
        }

        /// <summary>
        /// checks and returns the neighbours of a kdtree from a specific position in a specified radius
        /// </summary>
        /// <param name="pTree">the kd tree</param>
        /// <param name="pPosition">the position to check</param>
        /// <param name="radius">the radius neighbours are in</param>
        /// <returns>an enumerable with the points</returns>
        static NearestNeighbour<Point> lookForNeighbours(KDTree<Point> pTree, double[] pPosition, float radius)
        {
            return pTree.NearestNeighbors(pPosition, 100000, radius);
        }
    }
}
