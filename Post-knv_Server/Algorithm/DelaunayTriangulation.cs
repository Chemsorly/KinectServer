using Post_knv_Server.DataIntegration;
using Point = Post_knv_Server.DataIntegration.PointCloud.Point;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MIConvexHull;

namespace Post_knv_Server.Algorithm
{
    /// <summary>
    /// class that contains methods to perform a Volume calculation with the delaunay triangulation
    /// </summary>
    static class DelaunayTriangulation
    {
        /// <summary>
        /// calculates the delaunay volume of a given point cloud
        /// </summary>
        /// <param name="pointCloud">the point cloud</param>
        /// <returns>the volume of the point cloud</returns>
        public static float calculateDelaunayVolume(PointCloud pointCloud)
        {
            if (pointCloud.count < 5) return 0;

            //updates the status
            Log.LogManager.updateAlgorithmStatus("Delaunay Volume");

            //vars           
            float returnValue = 0;

            try
            {
                //create delaunay
                List<Tetrahedron> tetrahedrons = createDelaunayTetrahedrons(pointCloud);

                //calculate volume of tetrahedons
                //http://en.wikipedia.org/wiki/Tetrahedron#Volume
                foreach (Tetrahedron c in tetrahedrons)
                {
                    returnValue += ((float)Math.Abs(
                        determinant3x3(substract(c.Vertices[0], c.Vertices[1]),
                                        substract(c.Vertices[1], c.Vertices[2]),
                                        substract(c.Vertices[2], c.Vertices[3]))
                        ) / 6);
                }
            }
            catch (Exception) { return 0;}

            //updates the status
            Log.LogManager.updateAlgorithmStatus("Done");

            return returnValue;
        }

        /// <summary>
        /// creates a set of tetrahedrons from a given point cloud
        /// </summary>
        /// <param name="pPointCloud">the point cloud</param>
        /// <returns>a list of tetrahedrons</returns>
        static List<Tetrahedron> createDelaunayTetrahedrons(PointCloud pPointCloud)
        {
            //vars           
            List<Tetrahedron> retVal = new List<Tetrahedron>();
            try
            {
                //create vertex list
                HashSet<Vertex> vertices = new HashSet<Vertex>();
                foreach (Point p in pPointCloud.pointcloud_hs)
                    vertices.Add(new Vertex(p.point.X, p.point.Y, p.point.Z));
                retVal = Triangulation.CreateDelaunay<Vertex, Tetrahedron>(vertices).Cells.ToList();
            }
            catch (Exception) { throw; }

            return retVal;
        }

        /// <summary>
        /// creates a set of convex faces from a list of points
        /// </summary>
        /// <param name="pPointCloud">the list of points</param>
        /// <returns>a list of convex faces</returns>
        public static List<Face> createConvexFaces(List<Post_knv_Server.Algorithm.PlanarVolumeCalculation.TPoint> pPointCloud)
        {
            List<Face> retList = new List<Face>();

            try
            {
                List<Vertex> vertices = new List<Vertex>();
                foreach (Post_knv_Server.Algorithm.PlanarVolumeCalculation.TPoint p in pPointCloud)
                    vertices.Add(new Vertex(p.point.point.X, p.point.point.Y, p.point.point.Z));

                List<Vertex> convexHullVertices;
                ConvexHull<Vertex, Face> convexHull = ConvexHull.Create<Vertex, Face>(vertices);
                convexHullVertices = convexHull.Points.ToList();
                retList = convexHull.Faces.ToList();
            }
            catch (Exception)  { throw; }

            return retList;
        }

        /// <summary>
        /// returns a list of points of tetrahedrons in a given point cloud
        /// </summary>
        /// <param name="pointCloud">input point cloud</param>
        /// <returns>list of tetrahedrons</returns>
        public static List<List<Point>> calculateDelaunayTrianglePoints(PointCloud pointCloud)
        {
            List<List<Point>> retList = new List<List<Point>>();
            try
            {
                //create vertex list
                HashSet<Vertex> vertices = new HashSet<Vertex>();
                foreach (Point p in pointCloud.pointcloud_hs) { vertices.Add(new Vertex(p.point.X, p.point.Y, p.point.Z)); }

                //calculate tetrahedrons
                var tetrahedrons = Triangulation.CreateDelaunay<Vertex, Tetrahedron>(vertices).Cells;

                foreach (var c in tetrahedrons)
                {
                    List<Point> pL = new List<Point>();
                    for(int i = 0; i < 4; i++)
                    {
                        Point p = new Point(new ANX.Framework.Vector3((float)c.Vertices[i].Position[0], (float)c.Vertices[i].Position[1], (float)c.Vertices[i].Position[2]));
                        pL.Add(p);
                    }
                    retList.Add(pL);
                }
            }
            catch (Exception) { throw; }
            return retList;
        }

        /// <summary>
        /// calculates the determinant from 3 vectors
        /// </summary>
        /// <param name="a">vector 1</param>
        /// <param name="b">vector 2</param>
        /// <param name="c">vector 3</param>
        /// <returns>the determinant</returns>
        static float determinant3x3(float[] a, float[] b, float[] c)
        {
            return (a[0] * (b[1] * c[2] - b[2] * c[1]) -
                    b[0] * (a[1] * c[2] - a[2] * c[1]) +
                    c[0] * (a[1] * b[2] - a[2] * b[1]));
        }

        /// <summary>
        /// substracts 2 vectors
        /// </summary>
        /// <param name="a">vector 1</param>
        /// <param name="b">vector 2</param>
        /// <returns>difference vector</returns>
        static float[] substract(Vertex a, Vertex b)
        {
            float[] res = new float[3];
            res[0] = (float)(a.Position[0] - b.Position[0]);
            res[1] = (float)(a.Position[1] - b.Position[1]);
            res[2] = (float)(a.Position[2] - b.Position[2]);
            return res;
        }
    }
}
