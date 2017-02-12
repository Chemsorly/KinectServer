using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Point = Post_knv_Server.DataIntegration.PointCloud.Point;
using Vector3 = ANX.Framework.Vector3;
using Plane = ANX.Framework.Plane;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Post_knv_Server.DataIntegration
{
    /// <summary>
    /// plane model that represents a plane
    /// </summary>
    [Serializable()]
    public class PlaneModel
    {
        /// <summary>
        /// the ANX framework plane data model
        /// </summary>
        public Plane anxPlane { get; private set; }

        /// <summary>
        /// first point of the plane
        /// </summary>
        public Vector3 point1 { get; private set; }

        /// <summary>
        /// second point of the plane
        /// </summary>
        public Vector3 point2 { get; private set; }

        /// <summary>
        /// third point of the plane
        /// </summary>
        public Vector3 point3 { get; private set; }

        /// <summary>
        /// amount of inliers on the plane
        /// </summary>
        public int inliers { get; set; }

        /// <summary>
        /// is the plane marked as floor plane
        /// </summary>
        public bool isFloor { get; set; }

        /// <summary>
        /// constructor for plane
        /// </summary>
        /// <param name="pPoint1">point 1</param>
        /// <param name="pPoint2">point 2</param>
        /// <param name="pPoint3">point 3</param>
        public PlaneModel(Vector3 pPoint1, Vector3 pPoint2, Vector3 pPoint3)
        {
            this.anxPlane = new Plane(pPoint1, pPoint2, pPoint3);
            this.point1 = pPoint1;
            this.point2 = pPoint2;
            this.point3 = pPoint3;
            this.inliers = -1;
            this.isFloor = false;
        }

        /// <summary>
        /// compares two planes for similarity. both normal and offset need to be similar
        /// </summary>
        /// <param name="pPlaneA">first plane</param>
        /// <param name="pPlaneB">second plane</param>
        /// <param name="varianceValue">allowed variance threshold</param>
        /// <returns>true if similar, false if not</returns>
        public static bool comparePlanes(PlaneModel pPlaneA, PlaneModel pPlaneB, float varianceValue)
        {
            if (Math.Abs(Math.Abs(pPlaneA.anxPlane.Normal.X) - Math.Abs(pPlaneB.anxPlane.Normal.X)) < varianceValue &&
                Math.Abs(Math.Abs(pPlaneA.anxPlane.Normal.Y) - Math.Abs(pPlaneB.anxPlane.Normal.Y)) < varianceValue &&
                Math.Abs(Math.Abs(pPlaneA.anxPlane.Normal.Z) - Math.Abs(pPlaneB.anxPlane.Normal.Z)) < varianceValue &&
                Math.Abs(Math.Abs(pPlaneA.anxPlane.D) - Math.Abs(pPlaneB.anxPlane.D)) < varianceValue * 2)
                return true;
            return false;
        }

        /// <summary>
        /// compares the distance of a plane to a threshold distance. if two points distance is below threshold, it returns true
        /// </summary>
        /// <param name="pPlane">the plane containing 3 points</param>
        /// <param name="pDistance">threshold distance</param>
        /// <returns>true if below threshold</returns>
        public static bool comparePlanePoints(PlaneModel pPlane, float pDistance)
        {
            if(PointCloud.distanceBetweenPoints(pPlane.point1, pPlane.point2) < pDistance ||
               PointCloud.distanceBetweenPoints(pPlane.point2, pPlane.point3) < pDistance ||
               PointCloud.distanceBetweenPoints(pPlane.point3, pPlane.point1) < pDistance) return true;
            return false;
        }

        /// <summary>
        /// calculates the amount of inliers of the plane based on the provided point cloud
        /// </summary>
        /// <param name="pPointcloud">the point cloud</param>
        /// <param name="pPlane">the plane</param>
        /// <param name="pDistanceThreshold">inlier distance</param>
        /// <returns>amount of inliers</returns>
        public void calculateInliers(PointCloud pPointcloud, float pDistanceThreshold)
        {
            int resultValue = 0;
            foreach (Post_knv_Server.DataIntegration.PointCloud.Point p in pPointcloud.pointcloud_hs)
            {
                float dis = PointCloud.calculateDistancePointToPlane(p, this.anxPlane);
                if ( dis <= pDistanceThreshold)
                    resultValue++;
            }
            this.inliers = resultValue;
        }

        /// <summary>
        /// returns the plane points as a list
        /// </summary>
        /// <returns>plane point list</returns>
        public List<Point> getPointList()
        {
            List<Point> retList = new List<Point>();
            retList.Add(new Point(this.point1));
            retList.Add(new Point(this.point2));
            retList.Add(new Point(this.point3));
            return retList;
        }
    }
}
