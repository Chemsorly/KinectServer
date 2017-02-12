using KDTree;
using Microsoft.Kinect.Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using MoreLinq;
using Post_KNV_MessageClasses;
using Plane = ANX.Framework.Plane;
using PointCloudPicturePackage = Post_knv_Server.DataIntegration.PointCloudDrawing.PointCloudPicturePackage;
using System.IO;

namespace Post_knv_Server.DataIntegration
{
    /// <summary>
    /// class that represents a point cloud
    /// </summary>
    [Serializable()]
    public class PointCloud
    {
        #region external

        //kd tree representation; lazy loading necessary
        [NonSerialized]
        private KDTree<Point> _pointcloud_kd;
        public KDTree<Point> pointcloud_kd
        {
            get
            {
                //checks if kd tree has been calculated, if not, calculate, if yes, return
                if (_pointcloud_kd == null) _pointcloud_kd = createKDTreeFromHashset(this.pointcloud_hs);
                return _pointcloud_kd;
            }
        }

        //hashset representation, main list to work on
        public HashSet<Point> pointcloud_hs { get; private set; }
        public int count { get { return pointcloud_hs.Count; } }

        //pictures of point cloud; lazy loading
        [NonSerialized]
        private PointCloudPicturePackage _pictures;
        public PointCloudPicturePackage pictures
        {
            get
            {
                //checks if pictures already have been calculated, if not, calculate
                if (_pictures == null) _pictures = PointCloudDrawing.createPointCloudPicturePackageFromPointCloud(this, 424, 512);
                return _pictures;
            }
        }

        //the delaunay volume of the point cloud; lazy loading
        private float _delaunayVolume = float.MinValue;
        public float delaunayVolume
        {
            get
            {
                if (_delaunayVolume == float.MinValue) this._delaunayVolume = Algorithm.DelaunayTriangulation.calculateDelaunayVolume(this);
                return _delaunayVolume;
            }
        }

        /// <summary>
        /// the config object related to the point cloud (doesnt count for aligned ones)
        /// </summary>
        public ClientConfigObject ConfigObject { get; set; }

        /// <summary>
        /// constructor, creates a hashset of points from the mesh
        /// </summary>
        /// <param name="pVolume">the current reconstruction volume</param>
        public PointCloud(ColorReconstruction pVolume)
        {
            //takes the recontruction volume, exports the mesh and extracts the pointcloud from it
            ColorMesh mesh = pVolume.CalculateMesh(1);
            IReadOnlyCollection<Vector3> temp = mesh.GetVertices();

            //create a hashset
            pointcloud_hs = createHashset(temp);
        }

        /// <summary>
        /// create a new PointCloud object based on a hashset
        /// </summary>
        /// <param name="pInputPoints">the hashset</param>
        public PointCloud(HashSet<Point> pInputPoints)
        {
            this.pointcloud_hs = pInputPoints;
        }

        /// <summary>
        /// creates a new PointCloud from a mesh file
        /// </summary>
        /// <param name="filePath">the filepath</param>
        public PointCloud(String filePath)
        {
            this.pointcloud_hs = createHashsetFromSTLStream(filePath);
        }

        /// <summary>
        /// adds a point cloud to the existing one
        /// </summary>
        /// <param name="addingCloud">the point cloud to be added</param>
        public void addPointcloudToReference(PointCloud addingCloud, double[,] transformationMatrix, bool useICP, int inlierDistance)
        {
            PointCloudIntegration.integratePointClouds(this, addingCloud, transformationMatrix, useICP, inlierDistance);
            resetLazyLoadingObjectsAfterUpdate();
        }

        /// <summary>
        /// downsamples the pointcloud by using a downsample factor. Reduces amount of points in downsample factor distance to 1
        /// </summary>
        /// <param name="pDownsampleFactor">the downsample factor, a distance in m</param>
        public void downsamplePointcloud(float pDownsampleFactor)
        {
            //new target cloud
            HashSet<Point> newHS = new HashSet<Point>();

            //check all initial points
            foreach (Point p in pointcloud_hs)
            {
                //if not processed, add to new cloud, check neighbours and set them as processed; processed points dont get added to new pointcloud
                if (!p.processed)
                {
                    newHS.Add(p);
                    p.processed = true;

                    //check for nearest neighbours, set them processed, so they dont get added to list
                    NearestNeighbour<Point> nn = pointcloud_kd.NearestNeighbors(new double[] { p.point.X, p.point.Y, p.point.Z }, 10000, pDownsampleFactor);
                    while (nn.MoveNext())
                    {
                        if (!nn.Current.processed) nn.Current.processed = true;
                    }
                }
            }

            //goes through the point list and set every point as unprocessed
            Parallel.ForEach(newHS, point => point.processed = false);

            //set downsampled cloud as new
            this.pointcloud_hs = newHS;

            //resets the objects so they represent the current data
            this.resetLazyLoadingObjectsAfterUpdate();
        }

        /// <summary>
        /// removes all points within a distance from a plane
        /// </summary>
        /// <param name="pInputPlanes">the plane</param>
        /// <param name="pRemoveDistance">the distance</param>
        public void removePlaneFromPointcloud(List<PlaneModel> pInputPlanes, float pRemoveDistance)
        {
            foreach(PlaneModel pl in pInputPlanes)
                this.pointcloud_hs.RemoveWhere(p => calculateDistancePointToPlane(p, pl.anxPlane) < pRemoveDistance);
            resetLazyLoadingObjectsAfterUpdate();
        }

        /// <summary>
        /// removes a list of multiple objects from the point cloud by a distance
        /// </summary>
        /// <param name="pInputPoints">the list of removable point clouds</param>
        /// <param name="pRemoveDistance">the nearest neighbour search distance</param>
        /// <returns>the amount of points removed</returns>
        public int removePointcloudsFromPointCloud(List<PointCloud> pInputPoints, float pRemoveDistance)
        {
            Object parWork = new Object();
            HashSet<Point> removeList = new HashSet<Point>();

            //go through list // parallel
            Parallel.ForEach(pInputPoints, pcl =>
            {
                Parallel.ForEach(pcl.pointcloud_hs, p =>
                {
                    NearestNeighbour<Point> nn = pointcloud_kd.NearestNeighbors(new double[] { p.point.X, p.point.Y, p.point.Z }, 10000, pRemoveDistance);
                    while (nn.MoveNext())
                    {
                        lock (parWork) removeList.Add(nn.Current);
                    }
                });
            });

            //remove points from pointcloud
            int removecount = 0;
            foreach (Point p in removeList)
                if (this.pointcloud_hs.Remove(p)) removecount++;

            resetLazyLoadingObjectsAfterUpdate();
            return removecount;
        }

        /// <summary>
        /// exports the pointcloud to a file so it can be loaded again later
        /// </summary>
        public void exportPointCloud()
        {
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            System.IO.FileStream stream = null;
            if (!System.IO.File.Exists(@"config\referenceCloud.dat")) System.IO.Directory.CreateDirectory(@"config\");
            try
            {
                using(stream = new System.IO.FileStream(@"config\referenceCloud.dat", System.IO.FileMode.Create))
                {
                    formatter.Serialize(stream, this);
                    stream.Flush();
                    stream.Close();
                }
            }
            catch (Exception ex) { Log.LogManager.writeLog("ERROR: " + ex.Message); }
            finally { if (stream != null) { stream.Close(); stream.Dispose(); } }
        }

        /// <summary>
        /// imports a point cloud from a given path
        /// </summary>
        /// <param name="filepath">filepath</param>
        /// <returns>the point cloud</returns>
        public static PointCloud importPointCloud(String filepath)
        {
            PointCloud _returnObject;
            using (System.IO.FileStream stream = new System.IO.FileStream(filepath, System.IO.FileMode.Open))
            {
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                _returnObject = (PointCloud)formatter.Deserialize(stream);
            }

            return _returnObject;
        }

        /// <summary>
        /// point class that holds the vector3, including a parameter if it has been processed in an algorithm (default: false)
        /// </summary>
        [Serializable()]
        public class Point
        {
            //has the point been processed?
            internal bool processed { get; set; }

            //the point
            internal Vector3 point { get; private set; }

            /// <summary>
            /// constructor for Kinect.Fusion.Vector3
            /// </summary>
            /// <param name="pInputPoint">Kinect.Fusion.Vector3</param>
            internal Point(Vector3 pInputPoint)
            {
                this.processed = false;
                this.point = pInputPoint;
            }

            /// <summary>
            /// constructor for ANX.Framework.Vector3
            /// </summary>
            /// <param name="pInputPoint">ANX.Framework.Vector3</param>
            internal Point (ANX.Framework.Vector3 pInputPoint)
            {
                this.processed = false;
                Vector3 v = new Vector3();
                v.X = pInputPoint.X;
                v.Y = pInputPoint.Y;
                v.Z = pInputPoint.Z;
                this.point = v;
            }
        }

        #endregion

        #region internal

        /// <summary>
        /// resets the lazy loading objects after the point cloud has been updated (e.g. after downsampling)
        /// </summary>
        void resetLazyLoadingObjectsAfterUpdate()
        {
            this._pointcloud_kd = null;
            this._pictures = null;
            this._delaunayVolume = float.MinValue;
        }

        /// <summary>
        /// creates a kd tree based on the input cloud
        /// </summary>
        /// <param name="pInputcloud">the input cloud</param>
        /// <returns>the kd tree</returns>
        static KDTree<Point> createKDTreeFromHashset(HashSet<Point> pInputcloud)
        {
            KDTree<Point> resTree = new KDTree<Point>(3);
            foreach (Point v in pInputcloud)
            {
                //create point in tree
                resTree.AddPoint(new double[3] { v.point.X, v.point.Y, v.point.Z }, v);
            }

            return resTree;
        }

        /// <summary>
        /// creates a hashset from the input cloud
        /// </summary>
        /// <param name="pInputcloud">the input cloud</param>
        /// <returns>the hashset</returns>
        private static HashSet<Point> createHashset(IReadOnlyCollection<Vector3> pInputcloud)
        {
            HashSet<Point> resSet = new HashSet<Point>();
            foreach (Vector3 p in pInputcloud)
            {
                resSet.Add(new Point(p));
            }
            return resSet;
        }

        /// <summary>
        /// reads a *stl Mesh from a input stream and transforms it into a Hashset
        /// </summary>
        /// <param name="inputPath">the input stream from a stl file</param>
        /// <returns>the vertices of said mesh</returns>
        private static HashSet<Point> createHashsetFromSTLStream(String inputPath)
        {
            Log.LogManager.updateAlgorithmStatus("Importing STL Mesh");
            HashSet<Point> reSet = new HashSet<Point>();
            Object lockObject = new Object();

            byte[] file = File.ReadAllBytes(inputPath);
            int triangles = BitConverter.ToInt32(file, 80);
            Parallel.For(0, triangles, i =>
                {
                    for(int j = 0; j < 3; j++)
                    {
                        Vector3 p = new Vector3();
                        p.X = BitConverter.ToSingle(file, 84 + (i * 50) + 12 + (j * 12));
                        p.Y = BitConverter.ToSingle(file, 84 + (i * 50) + 12 + (j * 12) +4);
                        p.Z = BitConverter.ToSingle(file, 84 + (i * 50) + 12 + (j * 12) +8);
                        lock (lockObject) reSet.Add(new Point(p));
                    }
                });

            return reSet;
        }

        /// <summary>
        /// calculates the distance of a point from a plane in m
        /// </summary>
        /// <param name="pInputPoint">the point to check</param>
        /// <param name="pInputPlane">the input plane</param>
        /// <returns>the distance</returns>
        public static float calculateDistancePointToPlane(Point pInputPoint, Plane pInputPlane)
        {
            ANX.Framework.Vector3 point = new ANX.Framework.Vector3(pInputPoint.point.X, pInputPoint.point.Y, pInputPoint.point.Z);
            float dot = ANX.Framework.Vector3.Dot(pInputPlane.Normal, point);
            float value = dot - pInputPlane.D;

            return Math.Abs(value);
        }

        /// <summary>
        /// returns the distance between two points
        /// </summary>
        /// <param name="pointA">point a</param>
        /// <param name="pointB">point b</param>
        /// <returns>distance between point a and b in meters</returns>
        public static float distanceBetweenPoints(ANX.Framework.Vector3 pointA, ANX.Framework.Vector3 pointB)
        {
            return Math.Abs(ANX.Framework.Vector3.Distance(pointA, pointB));
        }

        #endregion

        /* (not needed atm)
        public static float distanceBetweenPoints(Vector3 pointA, Vector3 pointB)
        {
            ANX.Framework.Vector3 vna = new ANX.Framework.Vector3(pointA.X, pointA.Y, pointA.Z);
            ANX.Framework.Vector3 vnb = new ANX.Framework.Vector3(pointB.X, pointB.Y, pointB.Z);
            return distanceBetweenPoints(vna,vnb);
        }*/
    }
}
