using ANX.Framework;
using MIConvexHull;
using Post_knv_Server.DataIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Point = Post_knv_Server.DataIntegration.PointCloud.Point;

namespace Post_knv_Server.Algorithm.Utility
{
    /// <summary>
    /// class that contains the 3d to 2d projection algorithm
    /// </summary>
    class _3Dto2Dprojection
    {
        /*http://stackoverflow.com/questions/18663755/how-to-convert-a-3d-point-on-a-plane-to-uv-coordinates         * 
         * Projects a 3d Point to 2d coordinates on a plane
         * 
         * n = [a, b, c]
         * u = normalize([b, -a, 0]) //Assuming that a != 0 and b != 0, otherwise use c.
         * v  = n x u //If n was normalized, t is normalized already.
         * 
         * u_coord = dot(u,[x0 y0 z0])
         * v_coord = dot(v,[x0 y0 z0])
         *

        public static Vertex Project3dPointOn2dPlane(Point pPoint, PlaneModel pPlane)
        {
            Vector3 n = pPlane.anxPlane.Normal;

            Vector3 temp;
            if (n.Y == 0) temp = new Vector3(n.Z, -n.X, 0);
            else if (n.X == 0) temp = new Vector3(n.Y, -n.Z, 0);
            else temp = new Vector3(n.Y, -n.X, 0);

            Vector3 u = Vector3.Normalize(temp);
            Vector3 v = Vector3.Cross(n, u);

            double u_coord = Vector3.Dot(u, new Vector3(pPoint.point.X, pPoint.point.Y, pPoint.point.Z));
            double v_coord = Vector3.Dot(v, new Vector3(pPoint.point.X, pPoint.point.Y, pPoint.point.Z));

            return new Vertex(u_coord, v_coord);
        } */

        /// <summary>
        /// calculates the corresponding point on a plane from a point
        /// </summary>
        /// <param name="plane">the plane</param>
        /// <param name="point">the point</param>
        /// <returns>the corresponding point</returns>
        public static ANX.Framework.Vector3 ClosestPointOnPlane(ANX.Framework.Plane plane, ANX.Framework.Vector3 point)
        {
            /*
            //Distance From Plane To Point
            float distance = ANX.Framework.Vector3.Dot(
                    ANX.Framework.Vector3.Normalize(plane.Normal), point) - plane.D;

            return point - (plane.Normal * distance);*/

            //get normal
            ANX.Framework.Vector3 n = plane.Normal;

            //get u vector based on rotation
            ANX.Framework.Vector3 temp;
            if (n.Y == 0) temp = new ANX.Framework.Vector3(n.Z, -n.X, 0);
            else if (n.X == 0) temp = new ANX.Framework.Vector3(n.Y, -n.Z, 0);
            else temp = new ANX.Framework.Vector3(n.Y, -n.X, 0);

            //get u and v vector
            ANX.Framework.Vector3 u = ANX.Framework.Vector3.Normalize(temp);
            ANX.Framework.Vector3 v = ANX.Framework.Vector3.Cross(n, u);

            float u_coord = ANX.Framework.Vector3.Dot(u, new ANX.Framework.Vector3(point.X, point.Y, point.Z));
            float v_coord = ANX.Framework.Vector3.Dot(v, new ANX.Framework.Vector3(point.X, point.Y, point.Z));

            return new ANX.Framework.Vector3(u_coord, v_coord, 0f);
        }

    }
}
