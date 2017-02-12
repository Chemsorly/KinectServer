using icp_net;
using Post_knv_Server.DataIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post_knv_Server.Algorithm
{
    static class PointCloudAlignment
    {
        /// <summary>
        /// aligns two point clouds using a transformation matrix. Additionally an ICP implementation can be used
        /// </summary>
        /// <param name="pReferencePointCloud">the reference cloud to align the adding cloud to</param>
        /// <param name="pAddingPointCloud">the cloud that is supposed to be aligned</param>
        /// <param name="pTransMat">the transformation matrix: 4x4 with [column,row] where [0,0] to [2,2] is the rotation and [3,0] to [3,2] the translation. The other fields are unused</param>
        /// <param name="pUseICP">boolean to check if ICP is supposed to be used</param>
        /// <param name="pIndis">inlier distance for ICP</param>
        /// <returns>a modified point cloud with the valies [i,{x,y,z}]</returns>
        public static double[,] alignPointClouds(PointCloud pReferencePointCloud, PointCloud pAddingPointCloud, double[,] pTransMat, bool pUseICP, int pIndis)
        {

            //log messages
            Log.LogManager.updateAlgorithmStatus("Point Cloud Alignment");
            Log.LogManager.writeLogDebug("[PointCloudAlignment] Use ICP: " + pUseICP + ", (ICP) In distance inliers : " + pIndis + ", Transformation Matrix: " + 
                pTransMat[0,0] + "," + pTransMat[1,0] + "," + pTransMat[2,0] + "," + pTransMat[3,0] + ";" +
            pTransMat[0,1] + "," + pTransMat[1,1] + "," + pTransMat[2,1] + "," + pTransMat[3,1] + ";" +
            pTransMat[0,2] + "," + pTransMat[1,2] + "," + pTransMat[2,2] + "," + pTransMat[3,2] + ";" +
            pTransMat[0,3] + "," + pTransMat[1,3] + "," + pTransMat[2,3] + "," + pTransMat[3,3] );

            //transform point clouds into format
            double[,] referenceCloudDouble = new double[pReferencePointCloud.count, 3];
            double[,] addingCloudDouble = new double[pAddingPointCloud.count, 3];

            Task t1 = Task.Factory.StartNew(() =>
            {
                int counterRef = 0;
                foreach (PointCloud.Point p in pReferencePointCloud.pointcloud_hs)
                {
                    referenceCloudDouble[counterRef, 0] = p.point.X;
                    referenceCloudDouble[counterRef, 1] = p.point.Y;
                    referenceCloudDouble[counterRef, 2] = p.point.Z;
                    counterRef++;
                }
            });

            Task t2 = Task.Factory.StartNew(() =>
            {
                int counterRef = 0;
                foreach (PointCloud.Point p in pAddingPointCloud.pointcloud_hs)
                {
                    addingCloudDouble[counterRef, 0] = p.point.X;
                    addingCloudDouble[counterRef, 1] = p.point.Y;
                    addingCloudDouble[counterRef, 2] = p.point.Z;
                    counterRef++;
                }
            });
            t1.Wait(); t2.Wait();

            //perform translation
            Parallel.For(0, addingCloudDouble.GetLength(0), i =>
            {
                //fetch point
                double[] point = new double[3];
                for (int a = 0; a < 3; a++)
                    point[a] = addingCloudDouble[i, a];

                //transform with matrix
                double[] pointNEW = transformPointWithMatrix(point, pTransMat);

                //write point
                for (int a = 0; a < 3; a++)
                    addingCloudDouble[i, a] = pointNEW[a];
            });

            ///*****************************
            ///***ITERATIVE CLOSEST POINT***
            ///*****************************
            if (pUseICP)
            {
                //perform icp
                //create reference icp object
                ManagedICP icpManager = new ManagedICP(referenceCloudDouble, pReferencePointCloud.count, 3);

                //fit
                double[,] R = new double[3, 3]; //rotation matrix
                R[0, 0] = 1.0;
                R[1, 1] = 1.0;
                R[2, 2] = 1.0;

                double[] t = new double[3]; //translation vector

                icpManager.fit(addingCloudDouble, pAddingPointCloud.count, R, t, pIndis);

                //add to matrix4
                double[,] rem = new double[4, 4];
                rem[0, 0] = 1; rem[1, 1] = 2; rem[2, 2] = 1; rem[3, 3] = 1;

                //add transformation matrix;
                rem[0, 0] = (float)R[0, 0]; rem[0, 1] = (float)R[0, 1]; rem[0, 2] = (float)R[0, 2];
                rem[1, 0] = (float)R[1, 0]; rem[1, 1] = (float)R[1, 1]; rem[1, 2] = (float)R[1, 2];
                rem[2, 0] = (float)R[2, 0]; rem[2, 1] = (float)R[2, 1]; rem[2, 2] = (float)R[2, 2];

                //add translation vector
                rem[3, 0] = (float)t[0]; rem[3, 1] = (float)t[1]; rem[3, 2] = (float)t[2];

                //set remaining fields
                rem[0, 3] = 0; rem[1, 3] = 0; rem[2, 3] = 0;

                //perform translation
                Parallel.For(0, addingCloudDouble.GetLength(0), i =>
                {
                    //fetch point
                    double[] point = new double[3];
                    for (int a = 0; a < 3; a++)
                        point[a] = addingCloudDouble[i, a];

                    //transform with matrix
                    double[] pointNEW = transformPointWithMatrix(point, rem);

                    //write point
                    for (int a = 0; a < 3; a++)
                        addingCloudDouble[i, a] = pointNEW[a];
                });
            }
            ///******************************

            //updates the status
            Log.LogManager.updateAlgorithmStatus("Done");

            return addingCloudDouble;
        }

        /// <summary>
        /// transforms a vector based on a 4x4 matrix
        /// </summary>
        /// <param name="pPoint">the point to be transformed</param>
        /// <param name="pMatrix">the matrix</param>
        /// <returns>transformed point</returns>
        private static double[] transformPointWithMatrix(double[] pPoint, double[,] pMatrix)
        {
            double[] result = new double[3];
            result[0] = pMatrix[0, 0] * pPoint[0] + pMatrix[1, 0] * pPoint[1] + pMatrix[2, 0] * pPoint[2] + pMatrix[3, 0];
            result[1] = pMatrix[0, 1] * pPoint[0] + pMatrix[1, 1] * pPoint[1] + pMatrix[2, 1] * pPoint[2] + pMatrix[3, 1];
            result[2] = pMatrix[0, 2] * pPoint[0] + pMatrix[1, 2] * pPoint[1] + pMatrix[2, 2] * pPoint[2] + pMatrix[3, 2];

            return result;            
        }

    }
}