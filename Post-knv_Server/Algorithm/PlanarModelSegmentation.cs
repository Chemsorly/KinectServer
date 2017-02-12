using Post_knv_Server.DataIntegration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using Plane = ANX.Framework.Plane;
using Point = Post_knv_Server.DataIntegration.PointCloud.Point;
using System.Threading;

namespace Post_knv_Server.Algorithm
{
    /// <summary>
    /// class that contains the planar model segmentation algorithm
    /// </summary>
    static class PlanarModelSegmentation
    {
        /// <summary>
        /// gets a list of different planes in a point cloud using a RANSAC approach
        /// </summary>
        /// <param name="pPointSet">the point set the planes are supposed to be extracted from</param>
        /// <param name="pIterationThreshold">the amount of iterations per plane</param>
        /// <param name="pPlanedistanceThreshold">the distance threshold for inlier calculation</param>
        /// <param name="pNumberOfPlanes">the amount of planes to be returned</param>
        /// <param name="pPlaneComparisonValue">a variance value for plane comparison</param>
        /// <returns>a list of planes</returns>
        public static List<PlaneModel> doPlanarModelIterative(PointCloud pPointSet, int pIterationThreshold, float pPlanedistanceThreshold, int pNumberOfPlanes, float pPlaneComparisonValue, CancellationToken pTaskToken)
        {
            Log.LogManager.updateAlgorithmStatus("Planar Model Segmentation");
            Log.LogManager.writeLogDebug("[PlanarModelSegmentation] Iteration Threshold: " + pIterationThreshold + ", PlaneDistance Threshold: " + pPlanedistanceThreshold + ", Amount of planes: " + pNumberOfPlanes + ", Plane comparison value: " + pPlaneComparisonValue);

            //updates the status
            Log.LogManager.updateAlgorithmStatus("Done");

            return doPlanarModelSegmentation(pPointSet, pIterationThreshold, pPlanedistanceThreshold, pNumberOfPlanes, pPlaneComparisonValue, pTaskToken);
        }

        /// <summary>
        /// calculates the best fit plane model using RANSAC
        /// </summary>
        /// <param name="pPointSet">the point set to get the plane from</param>
        /// <param name="pIterationThreshold">the amount of iterations to do</param>
        /// <param name="pPlanedistanceThreshold"></param>
        /// <returns></returns>
        static List<PlaneModel> doPlanarModelSegmentation(PointCloud pPointSet, int pIterationThreshold, float pPlanedistanceThreshold, int pNumberOfPlanes, float pPlaneComparisonVariance, CancellationToken pTaskToken)
        {
            //abort if requested
            pTaskToken.ThrowIfCancellationRequested();

            //create bag of models
            List<TModel> models = new List<TModel>();
            Object tLock = new Object();

            //iterate using multithreading
            Parallel.For(0, pIterationThreshold, t =>
                {
                    //create new model to watch
                    TModel currentModel = new TModel();
                    int inliers = 0;

                    //select 3 random points to generate plane from
                    Point[] randomPoints = new Point[3];
                    Random rand = new Random(t * 1024);
                    for (int i = 0; i < 3; i++)
                        randomPoints[i] = pPointSet.pointcloud_hs.ElementAt(rand.Next(pPointSet.count));


                    //create Plane
                    currentModel.plane = new PlaneModel(new ANX.Framework.Vector3(randomPoints[0].point.X, randomPoints[0].point.Y, randomPoints[0].point.Z),
                        new ANX.Framework.Vector3(randomPoints[1].point.X, randomPoints[1].point.Y, randomPoints[1].point.Z),
                        new ANX.Framework.Vector3(randomPoints[2].point.X, randomPoints[2].point.Y, randomPoints[2].point.Z));

                    //iterate through set and count inliers
                    foreach(Point p in pPointSet.pointcloud_hs)
                    {
                        //check for abortion
                        pTaskToken.ThrowIfCancellationRequested();

                        //check distance, if smaller than threshold, increase count
                        float dis = PointCloud.calculateDistancePointToPlane(p, currentModel.plane.anxPlane);
                        if (dis < pPlanedistanceThreshold)                       
                            inliers++;        
                    }

                    //add to models
                    currentModel.plane.inliers = inliers;
                    currentModel.inliers = inliers;
                    lock(tLock)
                        models.Add(currentModel);
                });
            
            //sort by inliers
            var mod = models.OrderByDescending(t => t.inliers);

            //check for abortion
            pTaskToken.ThrowIfCancellationRequested();

            //check list for existing plane models
            List<PlaneModel> resList = new List<PlaneModel>();
            foreach (TModel pl in mod)
            {
                bool found = false;
                foreach (PlaneModel pm in resList)
                {
                    if (PlaneModel.comparePlanes(pm, pl.plane, pPlaneComparisonVariance))
                    { found = true; break; }
                }

                //if not found, add
                if (!found)
                    //check for point validity
                    if (!PlaneModel.comparePlanePoints(pl.plane, pPlanedistanceThreshold * 20))
                        resList.Add(pl.plane);                   

                //if list big enough, break
                if (resList.Count >= pNumberOfPlanes)
                    break;
            }

            return resList;
        }

        /// <summary>
        /// struct for the model to check
        /// </summary>
        struct TModel
        {
            //the model to check
            internal PlaneModel plane;

            //the parameter to evaluate the models usefulnes
            internal int inliers;
        }
    }
}
