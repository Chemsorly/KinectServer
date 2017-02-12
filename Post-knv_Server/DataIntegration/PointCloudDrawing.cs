using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Point = Post_knv_Server.DataIntegration.PointCloud.Point;
using System.Windows.Media;

namespace Post_knv_Server.DataIntegration
{
    public static class PointCloudDrawing
    {
        /// <summary>
        /// takes an existing PointCloudPictures package and adds a visible plane onto it
        /// </summary>
        /// <param name="pPictures">the picture package</param>
        /// <param name="pPoints">the plane model</param>
        /// <returns>plane drawn onto the pictures</returns>
        public static PointCloudPicturePackage addTriangleToPicturePackage(PointCloudPicturePackage pPictures, List<List<Point>> pPoints)
        {
            //create writeable clones
            WriteableBitmap bottomview = pPictures.bottomview.Clone();
            WriteableBitmap frontview = pPictures.frontview.Clone();
            WriteableBitmap sideview = pPictures.sideview.Clone();

            foreach (List<Point> p in pPoints)
            {
                //calculate relative pose of plane points with 0 <= x,y,z <= 1
                float x1, y1, z1, x2, y2, z2, x3, y3, z3;//, x4, y4, z4;
                x1 = (p[0].point.X + Math.Abs(pPictures.borders.xmin)) / (Math.Abs(pPictures.borders.xmax) + Math.Abs(pPictures.borders.xmin));
                y1 = (p[0].point.Y + Math.Abs(pPictures.borders.ymin)) / (Math.Abs(pPictures.borders.ymax) + Math.Abs(pPictures.borders.ymin));
                z1 = (p[0].point.Z + Math.Abs(pPictures.borders.zmin)) / (Math.Abs(pPictures.borders.zmax) + Math.Abs(pPictures.borders.zmin));

                x2 = (p[1].point.X + Math.Abs(pPictures.borders.xmin)) / (Math.Abs(pPictures.borders.xmax) + Math.Abs(pPictures.borders.xmin));
                y2 = (p[1].point.Y + Math.Abs(pPictures.borders.ymin)) / (Math.Abs(pPictures.borders.ymax) + Math.Abs(pPictures.borders.ymin));
                z2 = (p[1].point.Z + Math.Abs(pPictures.borders.zmin)) / (Math.Abs(pPictures.borders.zmax) + Math.Abs(pPictures.borders.zmin));

                x3 = (p[2].point.X + Math.Abs(pPictures.borders.xmin)) / (Math.Abs(pPictures.borders.xmax) + Math.Abs(pPictures.borders.xmin));
                y3 = (p[2].point.Y + Math.Abs(pPictures.borders.ymin)) / (Math.Abs(pPictures.borders.ymax) + Math.Abs(pPictures.borders.ymin));
                z3 = (p[2].point.Z + Math.Abs(pPictures.borders.zmin)) / (Math.Abs(pPictures.borders.zmax) + Math.Abs(pPictures.borders.zmin));

                /*
                //in case of tetrahedron and not triangle
                if(p.Count > 3)
                {
                    x4 = (p[3].point.X + Math.Abs(pPictures.borders.xmin)) / (Math.Abs(pPictures.borders.xmax) + Math.Abs(pPictures.borders.xmin));
                    y4 = (p[3].point.Y + Math.Abs(pPictures.borders.ymin)) / (Math.Abs(pPictures.borders.ymax) + Math.Abs(pPictures.borders.ymin));
                    z4 = (p[3].point.Z + Math.Abs(pPictures.borders.zmin)) / (Math.Abs(pPictures.borders.zmax) + Math.Abs(pPictures.borders.zmin));
                }*/

                //draw triangles
                bottomview.FillTriangle((int)(x1 * bottomview.Width), (int)(z1 * bottomview.Height),
                     (int)(x2 * bottomview.Width), (int)(z2 * bottomview.Height),
                     (int)(x3 * bottomview.Width), (int)(z3 * bottomview.Height), Colors.DarkGreen);

                frontview.FillTriangle((int)(x1 * frontview.Width), (int)(y1 * frontview.Height),
                    (int)(x2 * frontview.Width), (int)(y2 * frontview.Height),
                    (int)(x3 * frontview.Width), (int)(y3 * frontview.Height), Colors.DarkGreen);

                sideview.FillTriangle((int)(z1 * sideview.Width), (int)(y1 * sideview.Height),
                    (int)(z2 * sideview.Width), (int)(y2 * sideview.Height),
                    (int)(z3 * sideview.Width), (int)(y3 * sideview.Height), Colors.DarkGreen);
            };

            //freeze
            bottomview.Freeze();
            frontview.Freeze();
            sideview.Freeze();

            //create package
            PointCloudPicturePackage package = new PointCloudPicturePackage();
            package.borders = pPictures.borders;
            package.bottomview = bottomview;
            package.frontview = frontview;
            package.sideview = sideview;

            //return
            return package;
        }

        /// <summary>
        /// creates three bitmaps based on a point cloud
        /// </summary>
        /// <param name="pInputCloud">the point cloud</param>
        /// <param name="pHeight">the height of the bitmap</param>
        /// <param name="pWidth">the width of the bitmap</param>
        /// <returns>the picture package containing top, bottom and front view</returns>
        public static PointCloudPicturePackage createPointCloudPicturePackageFromPointCloud(PointCloud pInputCloud, int pHeight, int pWidth)
        {
            //create data objects
            PointCloudPicturePackage resPack = new PointCloudPicturePackage();
            resPack.sideview = new WriteableBitmap(pWidth, pHeight, 96.0, 96.0, System.Windows.Media.PixelFormats.Pbgra32, null);
            resPack.bottomview = new WriteableBitmap(pWidth, pHeight, 96.0, 96.0, System.Windows.Media.PixelFormats.Pbgra32, null);
            resPack.frontview = new WriteableBitmap(pWidth, pHeight, 96.0, 96.0, System.Windows.Media.PixelFormats.Pbgra32, null);

            //clear bitmap and set initial color value
            resPack.sideview.Clear(System.Windows.Media.Colors.White);
            resPack.bottomview.Clear(System.Windows.Media.Colors.White);
            resPack.frontview.Clear(System.Windows.Media.Colors.White);

            //find absolute borders for point cloud
            PointCloudBorderPackage pcbp = findBordersAbsolute(pInputCloud);
            resPack.borders = pcbp;

            //draw stuff
            foreach(Point p in pInputCloud.pointcloud_hs)
            {
                //calculate relative location by norming: 0 <= x,y,z <= 1
                float x, y, z;
                x = (p.point.X + Math.Abs(pcbp.xmin)) / (Math.Abs(pcbp.xmax) + Math.Abs(pcbp.xmin));
                y = (p.point.Y + Math.Abs(pcbp.ymin)) / (Math.Abs(pcbp.ymax) + Math.Abs(pcbp.ymin));
                z = (p.point.Z + Math.Abs(pcbp.zmin)) / (Math.Abs(pcbp.zmax) + Math.Abs(pcbp.zmin));

                if (!(x >= 1 || y >= 1 || z >= 1))
                {
                    //draw picture: x,y for front; x,z for bottom; z,y for side;
                    resPack.frontview.SetPixel((int)(x * pWidth), (int)(y * pHeight), System.Windows.Media.Colors.DarkRed);
                    resPack.bottomview.SetPixel((int)(x * pWidth), (int)(z * pHeight), System.Windows.Media.Colors.DarkRed);
                    resPack.sideview.SetPixel((int)(z * pWidth), (int)(y * pHeight), System.Windows.Media.Colors.DarkRed);
                }
            }

            //return
            resPack.bottomview.Freeze(); resPack.frontview.Freeze(); resPack.sideview.Freeze();
            return resPack;
        }

        public class PointCloudPicturePackage { internal WriteableBitmap sideview; internal WriteableBitmap frontview; internal WriteableBitmap bottomview; internal PointCloudBorderPackage borders;}

        /// <summary>
        /// finds the absolute borders of a point cloud in terms of coordinates
        /// </summary>
        /// <param name="pInputCloud">the point cloud</param>
        /// <returns>a struct containing the borders</returns>
        public static PointCloudBorderPackage findBordersAbsolute(PointCloud pInputCloud)
        {
            PointCloudBorderPackage resPack = new PointCloudBorderPackage();
            foreach (Point p in pInputCloud.pointcloud_hs)
            {
                if (p.point.X < resPack.xmin) resPack.xmin = p.point.X;
                if (p.point.X > resPack.xmax) resPack.xmax = p.point.X;
                if (p.point.Y < resPack.ymin) resPack.ymin = p.point.Y;
                if (p.point.Y > resPack.ymax) resPack.ymax = p.point.Y;
                if (p.point.Z < resPack.zmin) resPack.zmin = p.point.Z;
                if (p.point.Z > resPack.zmax) resPack.zmax = p.point.Z;
            }
            return resPack;
        }

        /// <summary>
        /// package that contains the borders for a point cloud
        /// </summary>
        public class PointCloudBorderPackage { internal float xmin = 1000, xmax = -1000, ymin = 1000, ymax = -1000, zmin = 1000, zmax = -1000;}
    }
}
