using Post_knv_Server.CalculationService;
using Post_knv_Server.DataIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Point = Post_knv_Server.DataIntegration.PointCloud.Point;

namespace Post_knv_Server.Algorithm
{
    class PlanarVolumeCalculation
    {
        /// <summary>
        /// calculates a list of intermediate scan results from a given list of pointclouds with the planar volume calculation algorithm
        /// </summary>
        /// <param name="pInputContainers">list of input point clouds</param>
        /// <param name="pMasterPlane">the plane to orient the point clouds</param>
        /// <param name="pDownsampleFactor">a downsampling factor</param>
        /// <param name="pAngleThreshold">angle threshold for concave hull algorithm</param>
        /// <returns>a list of intermediate scan results</returns>
        public static List<IntermediateScanResultPackage> CalculateIntermediateScanresults(List<PointCloud> pInputContainers, PlaneModel pMasterPlane, double pDownsampleFactor, float pAngleThreshold)
        {
            Log.LogManager.updateAlgorithmStatus("Start Calculation");
            List<IntermediateScanResultPackage> retList = new List<IntermediateScanResultPackage>();
            Object retLock = new object();

            Parallel.ForEach(pInputContainers, container =>
                {
                    //create result package
                    IntermediateScanResultPackage intermediateResult = new IntermediateScanResultPackage();
                    List<TPoint> corPointOnPlaneDown = CalculateCorrespondingPointsOnPlaneDownsampled(container, pMasterPlane, pDownsampleFactor);

                    //set point cloud
                    Task t1 = Task.Factory.StartNew(() => intermediateResult.pointCloud = container);

                    //calculate average height                    
                    Task t2 = Task.Factory.StartNew(() => intermediateResult.averageHeight = corPointOnPlaneDown.Average(t => t.distanceToPlane));

                    //calculate convex area
                    Task t3 = Task.Factory.StartNew(() => intermediateResult.convexPlaneArea = (DelaunayTriangulation.createConvexFaces(corPointOnPlaneDown).Sum(t => calculateAreaOfTriangleIn3dSpace(t.Vertices[0].Position, t.Vertices[1].Position, t.Vertices[2].Position)) / 2));
                    
                    //calculate concav area
                    Task t4 = Task.Factory.StartNew(() => intermediateResult.concavPlaneArea = Utility.PolygonAreaCalculation.CalculateAreaFromPointcloud(corPointOnPlaneDown, pAngleThreshold));

                    //wait for tasks to be completed
                    Task.WaitAll(t1,t2,t3,t4);

                    //add to list
                    lock (retLock)
                        retList.Add(intermediateResult);
                });
            return retList;
        }


        /// <summary>
        /// calculates a list of points from a point cloud, projects them to a plane and downsamples the result to ensure an average distribution of points
        /// </summary>
        /// <param name="pInputContainer">the container point cloud</param>
        /// <param name="pFloor">the floor to project the points on</param>
        /// <param name="pDownSampleFactor">a downsample factor to refine the points</param>
        /// <returns>a list of points projected on the floor palane</returns>
        static List<TPoint> CalculateCorrespondingPointsOnPlaneDownsampled(PointCloud pInputContainer, PlaneModel pFloor, double pDownSampleFactor)
        {
            //get corresponding points on plane
            List<TPoint> corPointsOnPlane = new List<TPoint>();
            KDTree.KDTree<TPoint> corTree = new KDTree.KDTree<TPoint>(3);

            foreach (Point p in pInputContainer.pointcloud_hs)
            {
                //create point model, add point on plane and add distance to said plane
                TPoint tp;
                tp.point = new Point(Utility._3Dto2Dprojection.ClosestPointOnPlane(pFloor.anxPlane, new ANX.Framework.Vector3(p.point.X, p.point.Y, p.point.Z)));
                tp.distanceToPlane = PointCloud.calculateDistancePointToPlane(p, pFloor.anxPlane);
                corPointsOnPlane.Add(tp);
                corTree.AddPoint(new double[3] { tp.point.point.X, tp.point.point.Y, tp.point.point.Z }, tp);
            }

            //downsample where depth = max
            List<TPoint> corPointsOnPlaneDownsampled = new List<TPoint>();
            foreach (TPoint p in corPointsOnPlane)
            {
                if (!p.point.processed)
                {
                    //set current point as best fit
                    TPoint maxDepthPoint = p;
                    p.point.processed = true;

                    //check neighbours if better fit exists
                    KDTree.NearestNeighbour<TPoint> nn = corTree.NearestNeighbors(new double[] { p.point.point.X, p.point.point.Y, p.point.point.Z }, 100000, pDownSampleFactor <= 0 ? 0.01d : pDownSampleFactor);
                    while (nn.MoveNext())
                    {
                        if (!nn.Current.point.processed)
                        {
                            nn.Current.point.processed = true;
                            if (nn.Current.distanceToPlane > maxDepthPoint.distanceToPlane)
                                maxDepthPoint = nn.Current;
                        }
                    }

                    //add best fit to new model
                    corPointsOnPlaneDownsampled.Add(p);
                }
            }

            return corPointsOnPlaneDownsampled;
        }

        /// <summary>
        /// calculates the volume of a triangle in 3d space
        /// </summary>
        /// <param name="pPoint1">array of the first point</param>
        /// <param name="pPoint2">array of the 2nd point</param>
        /// <param name="pPoint3">arrar of the 3rd point</param>
        /// <returns>the triangle area in m²</returns>
        static double calculateAreaOfTriangleIn3dSpace(double[] pPoint1, double[] pPoint2, double[] pPoint3)
        {
            //set vars
            double[] p1p2 = new double[3];
            double[] p1p3 = new double[3];

            //fill vars
            for (int i = 0; i < 3; i++)
            {
                p1p2[i] = pPoint2[i] - pPoint1[i];
                p1p3[i] = pPoint3[i] - pPoint1[i];
            }

            //calculate triangle area 
            ///Math.Sqrt(Math.Pow((x2*y3 - x3*y2),2) + Math.Pow((x3*y1-x1*y3),2) + Math.Pow((x1*y2-x2*y1)²),2)
            ///http://math.stackexchange.com/questions/128991/how-to-calculate-area-of-3d-triangle
            return 0.5d * Math.Sqrt(Math.Pow((p1p2[1]*p1p3[2] - p1p2[2]*p1p3[1]),2) + 
                Math.Pow((p1p2[2]*p1p3[0]-p1p2[0]*p1p3[2]),2) + 
                Math.Pow((p1p2[0]*p1p3[1]-p1p2[1]*p1p3[0]),2));
        }

        /// <summary>
        /// the structure for a point model used for the algorithm
        /// </summary>
        internal struct TPoint
        { internal Point point; internal float distanceToPlane;}
    }
}
