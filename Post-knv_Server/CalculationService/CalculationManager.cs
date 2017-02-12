using KDTree;
using Post_knv_Server.Algorithm;
using Post_knv_Server.Config;
using Post_knv_Server.DataIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Text;
using System.Threading.Tasks;
using Point = Post_knv_Server.DataIntegration.PointCloud.Point;
using System.Threading;
using Post_KNV_MessageClasses;

namespace Post_knv_Server.CalculationService
{
    /// <summary>
    /// class that manages the algorithm useage, needs to be instantiated since it uses asynchronous calculations
    /// </summary>
    public class CalculationManager
    {
        #region external

        //cancellation token source
        CancellationTokenSource _CancelTokenSource;

        //event for results ready
        public delegate void OnResultPackageReady(ScanResultPackage pResult);
        public event OnResultPackageReady OnResultPackageEvent;

        public delegate void OnNewPointPicturesReady(DataIntegration.PointCloudDrawing.PointCloudPicturePackage pointPictures);
        public event OnNewPointPicturesReady OnNewPointPicturesEvent;

        /// <summary>
        /// requests cancelation
        /// </summary>
        public void requestCancelation()
        {
            _CancelTokenSource.Cancel();
        }

        /// <summary>
        /// ASYNC: calculates the results of the point cloud and returns a result package; throws OnResultPackageEvent when done, OnErrorEvent when an error occured
        /// </summary>
        /// <param name="pInputCloud">the input cloud</param>
        /// <param name="pAlgorithm">the algorithm to chose</param>
        /// <param name="pSettings">the configuration object</param>
        public void calculateResults(PointCloud pInputCloud, ServerConfigObject pConfig )
        {
            _CancelTokenSource = new CancellationTokenSource();
            Log.LogManager.updateAlgorithmStatus("Start Calculation Service");
            Task<ScanResultPackage> t = new Task<ScanResultPackage>(() =>
            {
                //remove calibrated planes
                pInputCloud.removePlaneFromPointcloud(pConfig.serverAlgorithmConfig.calibratedPlanes,pConfig.serverAlgorithmConfig.planar_ThresholdDistance);
                this.OnNewPointPicturesEvent(pInputCloud.pictures);

                //remove calibrated objects
                pInputCloud.removePointcloudsFromPointCloud(pConfig.serverAlgorithmConfig.calibratedObjects, pConfig.serverAlgorithmConfig.euclidean_ExtractionRadius);
                this.OnNewPointPicturesEvent(pInputCloud.pictures);

                //remove noise via euclidean cluster extraction, discard too small ones
                List<PointCloud> pointCloudList = EuclideanClusterExtraction.calculateEuclideanClusterExtraction(pInputCloud, pConfig.serverAlgorithmConfig.euclidean_ExtractionRadius, _CancelTokenSource.Token);
                pointCloudList.RemoveAll(pcl => pcl.delaunayVolume < pConfig.serverAlgorithmConfig.euclidean_MinimumVolume);
                if (pointCloudList.Count == 0) throw new Exception("No valid point clouds found!");

                //calculate items in result package
                _CancelTokenSource.Token.ThrowIfCancellationRequested();
                ScanResultPackage resultPack = new ScanResultPackage();
                PlaneModel floorPlane = pConfig.serverAlgorithmConfig.calibratedPlanes.Find(pl => pl.isFloor);
                
                //create concav and convex areas
                List<IntermediateScanResultPackage> intermediateResultList = Algorithm.PlanarVolumeCalculation.CalculateIntermediateScanresults(pointCloudList,
                    floorPlane,
                    pConfig.serverAlgorithmConfig.downsamplingFactor, 
                    pConfig.serverAlgorithmConfig.concav_angleThreshold);

                //sum up convex and concav volume
                resultPack.convexScannedAlgorithmVolume = 0d; resultPack.concavScannedAlgorithmVolume = 0d;
                foreach (IntermediateScanResultPackage p in intermediateResultList)
                {
                    resultPack.convexScannedAlgorithmVolume += p.convexPlaneArea * p.averageHeight;
                    resultPack.concavScannedAlgorithmVolume += p.concavPlaneArea * p.averageHeight;
                }

                //calculate best fit for container matching, check first if container calibration exist, aggregate result
                resultPack.containerResults = null; resultPack.estimatedPayloadVolume = 0;
                if(pConfig.calibratedContainers.Count > 0)
                {
                    List<ContainerCalculation.tModel> bestFitSolutions = ContainerCalculation.CalculateBestFitSolutions(intermediateResultList, pConfig.calibratedContainers);
                    resultPack.containerResults = new List<ScanResultPackage.ContainerResult>();

                    //go through the best fit solution for every point cloud
                    foreach (ContainerCalculation.tModel p in bestFitSolutions)
                    {
                        foreach (Post_knv_Server.CalculationService.ContainerCalculation.tContainerModel m in p.containerResults)
                        {
                            //check if container exists in containerResults, if not add, if yes, just add
                            if (resultPack.containerResults.Exists(q => q.containerType == m.containerType))
                                resultPack.containerResults.Find(q => q.containerType == m.containerType).amount += m.amountOfContainers;
                            else
                            {
                                ScanResultPackage.ContainerResult res = new ScanResultPackage.ContainerResult();
                                res.containerType = m.containerType; res.amount = m.amountOfContainers;
                                resultPack.containerResults.Add(res);
                            }

                            //add container volume
                            resultPack.estimatedPayloadVolume += m.containerType.containerVolume * m.amountOfContainers;
                        }
                    }

                    //accumulate rating
                    resultPack.containerAccuracy = bestFitSolutions.Average(i => i.pRating);

                    //estimate payload
                    resultPack.estimatedPayloadVolume = resultPack.concavScannedAlgorithmVolume - resultPack.estimatedPayloadVolume;
                }                    

                //create results                
                resultPack.timestamp = DateTime.Now;
                resultPack.algorithmUsed = pConfig.serverAlgorithmConfig.useAlgorithm;
                resultPack.numberOfClouds = pointCloudList.Count;
                resultPack.numberOfPoints = pointCloudList.Sum(p => p.count);
                resultPack.scannedDelaunayVolume = pointCloudList.Sum(p => p.delaunayVolume);
                resultPack.amountOfKinectsDataRequested = pInputCloud.ConfigObject.clientRequestObject.amountOfKinectsInRequest;

                return resultPack;
            },_CancelTokenSource.Token);

            //task finishing
            t.ContinueWith(calculationFinishedSuccessfully, TaskContinuationOptions.OnlyOnRanToCompletion);
            t.ContinueWith(exceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            t.Start();
        }

        #endregion

        #region internal

        /// <summary>
        /// gets called when an error occurs during the algorithm calculation
        /// </summary>
        /// <param name="pTask">the task with the error</param>
        void exceptionHandler(Task pTask)
        {
            Log.LogManager.updateAlgorithmStatus("ERROR: " + pTask.Exception.InnerException.Message);
            Log.LogManager.writeLog("[CalculationManager] ERROR: " + pTask.Exception.InnerException.Message);
            //OnErrorEvent(pTask.Exception.InnerException);
        }

        /// <summary>
        /// gets called when the calculation finished successfully
        /// </summary>
        /// <param name="pTask">the task containing the results</param>
        void calculationFinishedSuccessfully(Task<ScanResultPackage> pTask)
        {
            Log.LogManager.updateAlgorithmStatus("Calculation Done");
            OnResultPackageEvent(pTask.Result);
        }

        #endregion
    }
}
