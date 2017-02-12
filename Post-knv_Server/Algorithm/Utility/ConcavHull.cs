using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MIConvexHull;
using ANX.Framework;
using MoreLinq;
using Vector = Post_knv_Server.Algorithm.Utility.LineSegmentIntersection.Vector;

namespace Post_knv_Server.Algorithm.Utility
{
    /// <summary>
    /// algorithm to do the concav hull calculation
    /// </summary>
    static class ConcavHull
    {
        /*Source: http://www.it.uu.se/edu/course/homepage/projektTDB/ht13/project10/Project-10-report.pdf
            Data: list A with edges for the convex hull
            Result: list B with edges for a concave hull
            Sort list A after the length of the edges;
            while list A is not empty
                Select the longest edge e from list A;
                Remove edge e from list A;
                Calculate local maximum distance d for edges;
                if length of edge is larger than distance d
                    Find the point p with the smallest maximum angle a;
                    if angle a is small enough and point p is not on the boundary
                        Create edges e2 and e3 between point p and endpoints of edge e;
                        if edge e2 and e3 don't intersect any other edge
                            Add edge e2 and e3 to list A;
                            Set point p to be on the boundary;
                if edge e2 and e3 was not added to list A
                Add edge e to list B;
         * */

        /// <summary>
        /// performs the concave hull algorithm on a dataset
        /// </summary>
        /// <param name="pPoints">dataset</param>
        /// <param name="pMaxAngle">angle threshold</param>
        /// <returns>list of edges of the concave hull</returns>
        public static List<tEdge> PerformConcavHullCalculation(HashSet<Vertex> pPoints, double pMaxAngle)
        {
            //calculate convex hull
            List<DefaultConvexFace<Vertex>> convexFaces = CreateConvexHullFromDataset(pPoints);

            //DEBUG convex hull length
            double d = 0;
            foreach(DefaultConvexFace<Vertex> t in convexFaces)
                d += Math.Sqrt(Math.Pow((t.Vertices[0].Position[1] - t.Vertices[1].Position[1]), 2) + Math.Pow((t.Vertices[0].Position[0] - t.Vertices[1].Position[0]), 2));
            Console.WriteLine("Convex hull length: " + d + " m");
            //DEBUG END

            //create concave hull
            return CreateConvaveHullFromConvexEdges(convexFaces, pPoints, pMaxAngle);
        }

        /// <summary>
        /// creates the concave hull based on the convex hull and an angle threshold
        /// </summary>
        /// <param name="pConvexEdges">edges of convex hull</param>
        /// <param name="pPoints">the point cloud data set</param>
        /// <param name="pMaxAngle">angle threshold</param>
        /// <returns>list of concave hull edges</returns>
        static List<tEdge> CreateConvaveHullFromConvexEdges(List<DefaultConvexFace<Vertex>> pConvexEdges, HashSet<Vertex> pPoints, double pMaxAngle)
        {
            //initialize arrays
            List<tEdge> convexHull = new List<tEdge>();
            foreach(var v in pConvexEdges) convexHull.Add(new tEdge(v.Vertices[0], v.Vertices[1]));
            List<tEdge> concaveHull = new List<tEdge>();

            //iterate through convex hull until concave hull is found
            while (convexHull.Count > 0)
            {
                //find longest edge in convex hull list
                tEdge longestEdge = convexHull.MaxBy(t => t.length);

                //remove longest edge from convex hull
                convexHull.Remove(longestEdge);
                
                //find nearest point
                List<tEdge> edges = convexHull.Concat(concaveHull).ToList();
                tVector nextPoint = FindPointWithSmallestAngle(pPoints, longestEdge, edges, pMaxAngle);
                if(nextPoint == null) //if no next point found
                {
                    concaveHull.Add(longestEdge);
                    continue;
                }                        

                //create edges based on nearest point
                tEdge e2 = new tEdge(longestEdge.v1, nextPoint.vector);
                tEdge e3 = new tEdge(longestEdge.v2, nextPoint.vector);

                //check if e2 and e3 intersect
                bool intersect = false;
                foreach (tEdge e1 in edges)
                {
                    if (Intersects(e2, e1) || Intersects(e3, e1))
                    {
                        intersect = true;
                        break;
                    }
                }

                //add e2 and e3 to List A again, if no intersection
                if(intersect == false)
                {
                    convexHull.Add(e2); convexHull.Add(e3); continue;
                }              

                //if no concaviction found, add longest edge to concaveHull list
                concaveHull.Add(longestEdge);
            }

            return concaveHull;
        }

        /// <summary>
        /// internal model that represents an edge
        /// </summary>
        internal class tEdge 
        {
            internal Vertex v1 { get; private set; }
            internal Vertex v2 { get; private set; }
            internal double length
            {
                get
                {
                    //http://stackoverflow.com/questions/20455317/how-do-i-calculate-the-length-of-a-line-between-two-points
                    //Math.Sqrt(Math.Pow((end.Y - start.Y), 2) + Math.Pow((end.X - start.X), 2));
                    return Math.Sqrt(Math.Pow((v1.Position[1] - v2.Position[1]), 2) + Math.Pow((v1.Position[0] - v2.Position[0]), 2));
                }
            }
            internal tEdge(Vertex pV1, Vertex pV2) { this.v1 = pV1; this.v2 = pV2; }
        }

        /// <summary>
        /// internal model that represents an vector with an angle towards an edge
        /// </summary>
        internal class tVector
        {
            internal Vertex vector { get; private set; }
            internal double maxAngle { get; private set; }
            internal tVector(Vertex pVector, double pMaxAngle)
            {
                this.vector = pVector;
                this.maxAngle = pMaxAngle;
            }
        }

        /// <summary>
        /// finds the point with the smallest angle from a given dataset towards an edge
        /// </summary>
        /// <param name="pPoints">the dataset</param>
        /// <param name="pLine">the edge to calculate the angle towards</param>
        /// <param name="pBoundaries">boundaries</param>
        /// <param name="pMaxAngle">angle threshold</param>
        /// <returns></returns>
        static tVector FindPointWithSmallestAngle(HashSet<Vertex> pPoints, tEdge pLine, List<tEdge> pBoundaries, double pMaxAngle)
        {
            tVector bestFit = null;
            Object lockObject = new object();
            Parallel.ForEach(pPoints, v =>
            {
                //check if point is on concave hull
                if (!IsPointOnConcaveHull(v, pBoundaries))
                {
                    //calculate new potential edges
                    tEdge t1 = new tEdge(pLine.v1, v);
                    tEdge t2 = new tEdge(pLine.v2, v);

                    //check if local maximum < edge length
                    if (t1.length < pLine.length && t2.length < pLine.length)
                    {
                        //calculate the highest angle between lines and the new point
                        double maxAngle;
                        double angleA = Math.Abs(CalculateAngleBetweenTwoLines(pLine, t1));
                        double angleB = Math.Abs(CalculateAngleBetweenTwoLines(pLine, t2));
                        if (angleA > angleB) maxAngle = angleA;
                        else maxAngle = angleB;

                        //check if highest angle is smaller than threshold
                        if (maxAngle < pMaxAngle)
                        {
                            //check if bestFit has been initialized
                            tVector currentFit = new tVector(v, maxAngle);
                            lock (lockObject)
                                if (bestFit == null)
                                    bestFit = currentFit;

                            //check if current point has a better fit
                            lock (lockObject)
                                if (currentFit.maxAngle < bestFit.maxAngle)
                                    bestFit = currentFit;
                        }
                    }
                }
            });

            return bestFit;
        }

        /// <summary>
        /// checks if a point is one of the concave edge points
        /// </summary>
        /// <param name="pPoint">the points</param>
        /// <param name="pConcaveHull">the list of edges of the concave hull</param>
        /// <returns>true if one of the points equals the input point</returns>
        static bool IsPointOnConcaveHull(Vertex pPoint, List<tEdge> pConcaveHull)
        {
            foreach(tEdge t in pConcaveHull)
                if (pPoint == t.v1 || pPoint == t.v2)
                    return true;
            return false;
        }

        /// <summary>
        /// calculates the angle between two lines
        /// </summary>
        /// <param name="l1">line 1</param>
        /// <param name="l2">line 2</param>
        /// <returns>the angle between line 1 and line 2</returns>
        static double CalculateAngleBetweenTwoLines(tEdge l1, tEdge l2)
        {
            
            //https://social.msdn.microsoft.com/Forums/vstudio/en-US/fa0cfeb6-70b7-4181-bc9b-fe625cd5e159/angle-between-two-lines
            //Double Angle = Math.Atan2(y2-y1, x2-x1) - Math.Atan2(y4-y3,x4-x3);
            return Math.Atan2(l1.v2.Position[1] - l1.v1.Position[1], l1.v2.Position[0] - l1.v1.Position[0]) - Math.Atan2(l2.v2.Position[1] - l2.v1.Position[1], l2.v2.Position[0] - l2.v1.Position[0]);
            
            /*
            //http://usb.unitedsb.de/topic/72776-java-winkel-zwischen-2-geraden-berechnen-wie/
            //calculate new vectors // l1.v1 == l2.v1
            Vertex P1P2 = new Vertex(l1.v2.Position[0] - l1.v1.Position[0], l1.v2.Position[1] - l1.v1.Position[1]);
            Vertex P1P3 = new Vertex(l2.v2.Position[0] - l1.v1.Position[0], l2.v2.Position[1] - l1.v1.Position[1]);

            //calculate length of vectors
            double norm1 = Math.Sqrt((P1P2.Position[0] * P1P2.Position[0]) + (P1P2.Position[1] * P1P2.Position[1]));
            double norm2 = Math.Sqrt((P1P3.Position[0] * P1P3.Position[0]) + (P1P3.Position[1] * P1P3.Position[1]));

            //scalar product
            double skpr = (P1P2.Position[0] * P1P3.Position[0]) + (P1P2.Position[1] * P1P3.Position[1]);

            //calc angle (radial, bogenmaß)
            double alpha = Math.Acos(skpr / (norm1 * norm2));

            return ((180 / Math.PI) * alpha);*/
        }

        /// <summary>
        /// takes an unordered dataset containing Vector2 and creates the edges of the convex hull from it
        /// </summary>
        /// <param name="pDataSet">the dataset</param>
        /// <returns>the faces of the convex hull</returns>
        static List<DefaultConvexFace<Vertex>> CreateConvexHullFromDataset(HashSet<Vertex> pDataSet)
        {
            //calcuate and return faces
            return ConvexHull.Create(pDataSet).Faces.ToList();
        }

        /// <summary>
        /// checks if two edges are intersecting
        /// </summary>
        /// <param name="AB">edge 1</param>
        /// <param name="CD">edge 2</param>
        /// <returns>true if intersects, false if not</returns>
        static bool Intersects(tEdge AB, tEdge CD)
        {

            //http://www.codeproject.com/Tips/862988/Find-the-Intersection-Point-of-Two-Line-Segments
            Vector ABs = new Vector(AB.v1.Position[0], AB.v1.Position[1]);
            Vector ABe = new Vector(AB.v2.Position[0], AB.v2.Position[1]);
            Vector CDs = new Vector(CD.v1.Position[0], CD.v1.Position[1]);
            Vector CDe = new Vector(CD.v2.Position[0], CD.v2.Position[1]);

            Vector intersection;
            bool intersects = LineSegmentIntersection.LineSegementsIntersect(ABs, ABe, CDs, CDe, out intersection);

            if (intersects)
                if (intersection.Equals(ABs) || intersection.Equals(ABe) || intersection.Equals(CDs) || intersection.Equals(CDe))
                    return false;
            return intersects;
        }

    }

}
