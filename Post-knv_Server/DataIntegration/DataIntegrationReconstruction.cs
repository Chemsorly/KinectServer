using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Microsoft.Kinect.Fusion;
using Post_KNV_MessageClasses;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.IO;

namespace Post_knv_Server.DataIntegration
{
    /// <summary>
    /// Manager to manage the reconstruction with kinect fusion
    /// </summary>
    class DataIntegrationReconstruction
    {
        #region fields

        /// <summary>
        /// kinect fusion volume
        /// </summary>
        ColorReconstruction volume;

        /// <summary>
        /// lock object to ensure only one process works on the volume
        /// </summary>
        public Object canWorkLock {get; private set;} 
        
        /// <summary>
        /// intermediate storages for kinect fusion
        /// </summary>
        FusionFloatImageFrame depthFloatFrame;
        float[] depthFloatFrameDepthPixels;
        int[] depthFloatFramePixelsArgb;
        int trackingErrorCount;
        Matrix4 worldToCameraTransform;             //transformation between world and camera view coordinate system
        Matrix4 defaultWorldToVolumeTransform;      //default value for above

        #endregion

        #region external

        //the fusion picture ready event
        internal delegate void OnNewFusionPictureReady(int ID, WriteableBitmap fusionPicture);
        internal event OnNewFusionPictureReady OnNewFusionPictureEvent;
        
        internal delegate void OnNewPointCloudReady(PointCloud pcl);
        internal event OnNewPointCloudReady OnNewPointCloudEvent;

        /// <summary>
        /// constructor, creates new reconstruction volume
        /// </summary>
        internal DataIntegrationReconstruction()
        {
            this.canWorkLock = new Object();

            this.worldToCameraTransform = Matrix4.Identity;
            this.volume = ColorReconstruction.FusionCreateReconstruction(new ReconstructionParameters(
                    Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.VoxelsPerMeter, 
                    Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.VoxelResolutionX,
                    Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.VoxelResolutionY,
                    Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.VoxelResolutionZ),
                Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.ProcessorType,
                Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.DeviceToUse,
                worldToCameraTransform);
            this.defaultWorldToVolumeTransform = this.volume.GetCurrentWorldToVolumeTransform();

            depthFloatFrame = new FusionFloatImageFrame(Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.depthWidth,
                Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.depthHeight);
            depthFloatFrameDepthPixels = new float[depthFloatFrame.Width * depthFloatFrame.Height];
            depthFloatFramePixelsArgb = new int[depthFloatFrame.Width * depthFloatFrame.Height];
        }

        /// <summary>
        /// external function to accept the kinect data package; starts deparate thread to operate on the volume
        /// </summary>
        /// <param name="pKdp">the data package</param>
        internal void acceptKinectDataPackage(KinectDataPackage pKdp, System.Threading.CancellationToken pCancelToken)
        {
            Task.Factory.StartNew(() => processDepthData(pKdp, pCancelToken), pCancelToken);
        }

        #endregion

        #region internal

        /// <summary>
        /// processes the depth data package into the kinect fusion volume
        /// </summary>
        /// <param name="pKdp">the data package</param>
        void processDepthData(KinectDataPackage pKdp, System.Threading.CancellationToken pCancelToken)
        {
            lock (canWorkLock)
            {
                Log.LogManager.updateAlgorithmStatus("Kinect Fusion integration");
                this.volume.ResetReconstruction(Matrix4.Identity);

                int picturesIntegrated = 0;
                foreach (ushort[] pDepth in pKdp.rawDepthData)
                {
                    pCancelToken.ThrowIfCancellationRequested();
                    WriteableBitmap bitmap = new WriteableBitmap(this.depthFloatFrame.Width, this.depthFloatFrame.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                    FusionFloatImageFrame depthFloatBuffer = new FusionFloatImageFrame(this.depthFloatFrame.Width, this.depthFloatFrame.Height);
                    FusionPointCloudImageFrame pointCloudBuffer = new FusionPointCloudImageFrame(this.depthFloatFrame.Width, this.depthFloatFrame.Height);
                    FusionColorImageFrame shadedSurfaceColorFrame = new FusionColorImageFrame(this.depthFloatFrame.Width, this.depthFloatFrame.Height);
                    int[] voxelPixels = new int[this.depthFloatFrame.Width * this.depthFloatFrame.Height];

                    this.volume.DepthToDepthFloatFrame(
                                pDepth,
                                depthFloatBuffer,
                                Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.minDepthClip,
                                Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.maxDepthClip,
                                false);
                    float alignmentValue;
                    bool trackingSucceeded = this.volume.ProcessFrame(depthFloatBuffer,
                        Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.iterationCount,
                        Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.integrationWeight,
                        out alignmentValue,
                        volume.GetCurrentWorldToCameraTransform());

                    // If camera tracking failed, no data integration or raycast for reference
                    // point cloud will have taken place, and the internal camera pose
                    // will be unchanged.
                    if (!trackingSucceeded)
                    {
                        trackingErrorCount++;
                    }
                    else
                    {
                        Matrix4 calculatedCameraPose = volume.GetCurrentWorldToCameraTransform();

                        // Set the camera pose and reset tracking errors
                        worldToCameraTransform = calculatedCameraPose;
                        trackingErrorCount = 0;
                    }

                    // Calculate the point cloud
                    volume.CalculatePointCloud(pointCloudBuffer, worldToCameraTransform);

                    // Shade point cloud and render
                    FusionDepthProcessor.ShadePointCloud(
                        pointCloudBuffer,
                        worldToCameraTransform,
                        null,
                        shadedSurfaceColorFrame
                        );

                    shadedSurfaceColorFrame.CopyPixelDataTo(voxelPixels);

                    bitmap.WritePixels(
                        new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                        voxelPixels,
                        bitmap.PixelWidth * sizeof(int),
                        0);

                    bitmap.Freeze();
                    OnNewFusionPictureEvent.BeginInvoke(pKdp.usedConfig.ID, bitmap, null, null);

                    picturesIntegrated++;
                    Log.LogManager.writeLogDebug("[DataIntegration:Reconstruction] " + picturesIntegrated + " of " + pKdp.rawDepthData.Count + " Pictures integrated");                    
                }

                //if request was calibration request, export meshes
                if (pKdp.usedConfig.clientRequestObject.requestType == ClientConfigObject.RequestType.calibration)
                {
                    exportMesh(volume, pKdp, false);
                    Log.LogManager.writeLog("[DataIntegration:Reconstruction] Mesh of " + pKdp.usedConfig.name + " exported.");
                    return;
                }

                //broadcast new point cloud
                PointCloud p = new PointCloud(volume);
                p.ConfigObject = pKdp.usedConfig;
                OnNewPointCloudEvent.BeginInvoke(p,null,null);
                Log.LogManager.writeLog("[DataIntegration:Reconstruction] All pictures of " + pKdp.usedConfig.name + " integrated");
                Log.LogManager.updateAlgorithmStatus("Done");
            }
        }

        /// <summary>
        /// resets the reconstruction volume
        /// </summary>
        void resetReconstruction()
        {
            // Reset tracking error counter
            this.trackingErrorCount = 0;

            // Set the world-view transform to identity, so the world origin is the initial camera location.
            this.worldToCameraTransform = Matrix4.Identity;

            if (null != this.volume)
            {
                // Translate the reconstruction volume location away from the world origin by an amount equal
                // to the minimum depth threshold. This ensures that some depth signal falls inside the volume.
                // If set false, the default world origin is set to the center of the front face of the 
                // volume, which has the effect of locating the volume directly in front of the initial camera
                // position with the +Z axis into the volume along the initial camera direction of view.
                if (Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.translateResetPoseByMinDepthThreshold)
                {
                    Matrix4 worldToVolumeTransform = this.defaultWorldToVolumeTransform;

                    // Translate the volume in the Z axis by the minDepthThreshold distance
                    float minDepthClip = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.minDepthClip;
                    float maxDepthClip = Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.maxDepthClip;

                    float minDist = (minDepthClip < maxDepthClip) ? minDepthClip : maxDepthClip;
                    worldToVolumeTransform.M43 -= minDist * Config.ServerConfigManager._ServerConfigObject.serverKinectFusionConfig.VoxelsPerMeter;

                    this.volume.ResetReconstruction(this.worldToCameraTransform, worldToVolumeTransform); 
                }
                else
                {
                    this.volume.ResetReconstruction(this.worldToCameraTransform);
                }
            }

        }

        /// <summary>
        /// creates a mesh from the current volume and tries to save it to a file
        /// </summary>
        /// <param name="volume">the volume</param>
        /// <param name="pkdp">the data package the mesh origined from</param>
        /// <param name="flipAxes">should achses be flipped?</param>
        static void exportMesh(ColorReconstruction volume, KinectDataPackage pkdp, bool flipAxes)
        {
            ColorMesh mesh = volume.CalculateMesh(1);

            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.FileName = "KinectFusionMesh_" + pkdp.usedConfig.name + DateTime.UtcNow.ToShortDateString() + ".stl";
            dialog.Filter = "STL Mesh Files|*.stl|All Files|*.*";

            if (true == dialog.ShowDialog())
                {
                    using (BinaryWriter writer = new BinaryWriter(dialog.OpenFile()))
                    {
                        if (null == mesh || null == writer)
                        {
                            return;
                        }

                        var vertices = mesh.GetVertices();
                        var normals = mesh.GetNormals();
                        var indices = mesh.GetTriangleIndexes();

                        // Check mesh arguments
                        if (0 == vertices.Count || 0 != vertices.Count % 3 || vertices.Count != indices.Count)
                        {
                            throw new Exception("Invalid Mesh Arguments");
                        }

                        char[] header = new char[80];
                        writer.Write(header);

                        // Write number of triangles
                        int triangles = vertices.Count / 3;
                        writer.Write(triangles);

                        // Sequentially write the normal, 3 vertices of the triangle and attribute, for each triangle
                        for (int i = 0; i < triangles; i++)
                        {
                            // Write normal
                            var normal = normals[i * 3];
                            writer.Write(normal.X);
                            writer.Write(flipAxes ? -normal.Y : normal.Y);
                            writer.Write(flipAxes ? -normal.Z : normal.Z);

                            // Write vertices
                            for (int j = 0; j < 3; j++)
                            {
                                var vertex = vertices[(i * 3) + j];
                                writer.Write(vertex.X);
                                writer.Write(flipAxes ? -vertex.Y : vertex.Y);
                                writer.Write(flipAxes ? -vertex.Z : vertex.Z);
                            }

                            ushort attribute = 0;
                            writer.Write(attribute);
                        }
                    }
                }
        }

        #endregion
    }
}
