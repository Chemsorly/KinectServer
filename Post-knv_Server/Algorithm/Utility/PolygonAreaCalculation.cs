using ANX.Framework;
using MIConvexHull;
using Post_knv_Server.DataIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TPoint = Post_knv_Server.Algorithm.PlanarVolumeCalculation.TPoint;

namespace Post_knv_Server.Algorithm.Utility
{

    /// <summary>
    /// class that contains the Polygon Area Calculation algorithm via concave hull creation and polygon reconstruction
    /// </summary>
    class PolygonAreaCalculation
    {
        /// <summary>
        /// calculates the area projected on a point cloud to a plane
        /// </summary>
        /// <param name="pProjectedInputCloud">the projected point cloud on a plane</param>
        /// <returns>the area</returns>
        public static double CalculateAreaFromPointcloud(List<TPoint> pProjectedInputCloud, float pAngleThreshold)
        {
            //convert input cloud from TPoint to Vertex
            HashSet<Vertex> projectedConcaveInput = new HashSet<Vertex>();            
            Object lockObject = new Object();
            Parallel.ForEach(pProjectedInputCloud, p =>
                {                  
                    Vertex pNew = new Vertex(p.point.point.X,p.point.point.Y);
                    lock(lockObject)
                        projectedConcaveInput.Add(pNew);                    
                });

            //create concave hull and extract bounds
            var concaveHull = Utility.ConcavHull.PerformConcavHullCalculation(projectedConcaveInput, pAngleThreshold);

            //DEBUG: calculate concave hull length
            double f = concaveHull.Sum(c => c.length);
            Console.WriteLine("Concave hull length("+pProjectedInputCloud.Count+"): " + f.ToString() + " m");

            //order bounds
            var boundsCleaned = OrderTriangulationPoints(concaveHull);

            //triangulate the concave outer bounds
            Vertex[] pointArray = boundsCleaned.ToArray();
            Post_knv_Server.Algorithm.Utility.PolygonTriangulation.Polygon poly = new Utility.PolygonTriangulation.Polygon(pointArray);

            //calculate area of triangles
            double resultArea = Math.Abs(poly.PolygonArea());

            return resultArea;
        }

        /// <summary>
        /// takes a list of edges and orders the points of the edges
        /// </summary>
        /// <param name="pInputEdges">the edges</param>
        /// <returns>list of ordered points</returns>
        static List<Vertex> OrderTriangulationPoints(List<Utility.ConcavHull.tEdge> pInputEdges)
        {
            //create lists
            List<Vertex> resultList = new List<Vertex>();

            //add first point from edge to start with
            resultList.Add(pInputEdges[0].v1);

            //iterate until all edges used
            while (pInputEdges.Count > 0)
            {
                //take last point
                Vertex lastPoint = resultList.Last();

                //find edge that has point and is NOT on removed edge list (every point has 2 corresponding edges)
                ConcavHull.tEdge edge = pInputEdges.Find(t => (t.v1.Position[0] == resultList.Last().Position[0] && t.v1.Position[1] == resultList.Last().Position[1]) ||
                            (t.v2.Position[0] == resultList.Last().Position[0] && t.v2.Position[1] == resultList.Last().Position[1]));
                pInputEdges.Remove(edge);

                //check which was the last point, add the other point of the edge; throw exception if the edges are not closed
                if (edge.v1 == resultList.Last())
                {
                    resultList.Add(edge.v2);
                }
                else if (edge.v2 == resultList.Last())
                {
                    resultList.Add(edge.v1);
                }
                else throw new Exception("Edges are not a hull.");
            }

            return resultList;
        }
    }
}
