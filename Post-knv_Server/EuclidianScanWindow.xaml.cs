using Post_knv_Server.DataIntegration;
using Point = Post_knv_Server.DataIntegration.PointCloud.Point;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Post_knv_Server
{
    /// <summary>
    /// interaction logic for EuclidianScanWindow.xaml
    /// </summary>
    public partial class EuclidianScanWindow : Window
    {

        PointCloud refPointCloud;
        
        List<PointCloud> pointcloudList;
        List<PointCloud> refList;
        
        System.Threading.CancellationTokenSource _CancelTokenSource;

        int clustersTotal = 0;
        int clustersAboveLimit = 0;
        int pointsTotal = 0;
        int pointsAboveLimit = 0;

        float volumeTotal = 0f;
        float volumeAboveLimit = 0f;

        internal delegate void OnCloudListUpdatedReady(List<PointCloud> pc);
        internal event OnCloudListUpdatedReady OnCloudListUpdatedEvent;

        /// <summary>
        /// init of the euclidianscanwindow
        /// </summary>
        /// <param name="pc"></param>
        public EuclidianScanWindow(PointCloud pc, List<PointCloud> pl)
        {
            InitializeComponent();
            _CancelTokenSource = new System.Threading.CancellationTokenSource();            
            refPointCloud = pc;
            pointcloudList = new List<PointCloud>();
            
            foreach (PointCloud pla in pl) pointcloudList.Add(pla);

            refList = new List<PointCloud>();
            
            foreach (PointCloud refpl in pointcloudList) refList.Add(refpl);

            Task<List<PointCloud>> t = new Task<List<PointCloud>>(() =>
                {                    
                    List<PointCloud> euclCLusters = Algorithm.EuclideanClusterExtraction.calculateEuclideanClusterExtraction(refPointCloud, Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.euclidean_ExtractionRadius, _CancelTokenSource.Token);
                    return euclCLusters;
                }, _CancelTokenSource.Token);
            t.ContinueWith(EuclideanEnded, TaskContinuationOptions.OnlyOnRanToCompletion);
            t.Start();
        }

        /// <summary>
        /// gets called when the euclidean task is completed
        /// </summary>
        /// <param name="pTask">task object</param>
        void EuclideanEnded(Task<List<PointCloud>> pTask)
        {
            foreach (PointCloud pcl in pTask.Result) pointcloudList.Add(pcl);

            //stats pre
            clustersTotal = pointcloudList.Count;
            foreach (PointCloud pcl in pointcloudList) { volumeTotal += pcl.delaunayVolume; pointsTotal += pcl.count; }

            //alterate point cloud list and removal
            //List<PointCloud> pointcloudListMinor = pointcloudList.Where(c => c.delaunayVolume < Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.euclidean_MinimumVolume).ToList();
            //refPointCloud.removePointcloudsFromPointCloud(pointcloudListMinor, Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.euclidean_ExtractionRadius);
            pointcloudList.RemoveAll(c => c.delaunayVolume < Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.euclidean_MinimumVolume);
            
            //stats post
            clustersAboveLimit = pointcloudList.Count;
            foreach (PointCloud pcl in pointcloudList) { volumeAboveLimit += pcl.delaunayVolume; pointsAboveLimit += pcl.count; }
            
            UpdateListView();
        }

        /// <summary>
        /// function for the update of the listview & the stats
        /// </summary>
        private void UpdateListView()
        {
            this.Dispatcher.Invoke(() =>
            {
                EuclidianListView.Items.Clear();
                
                //add new items to the listview
                if (pointcloudList != null)
                {
                    var np = pointcloudList.OrderByDescending(t => t.delaunayVolume);
                    foreach (PointCloud pc in np)
                    {
                        bool origin = false;
                        if (refList.Exists(t => t.Equals(pc))) origin = true;
                        EuclidianListViewItem eLv = new EuclidianListViewItem() { pointcloud = pc, count = pc.count, cubicM = pc.delaunayVolume, selectCluster = origin };
                        EuclidianListView.Items.Add(eLv);
                    }

                }

                //update the textblocks for the stats
                _Textblock_ClustersAboveLimit.Text = clustersAboveLimit.ToString();
                _Textblock_ClustersTotal.Text = clustersTotal.ToString();
                _Textblock_ExtractionRadius.Text = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.euclidean_ExtractionRadius.ToString() +" m";
                _Textblock_ExtractionVolumeLimit.Text = Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.euclidean_MinimumVolume.ToString() +" m";
                _Textblock_PointsAboveLimit.Text = pointsAboveLimit.ToString();
                _Textblock_PointsTotal.Text = pointsTotal.ToString();
                _Textblock_VolumeAboveLimit.Text = volumeAboveLimit.ToString() +" m³";
                _Textblock_VolumeTotal.Text = volumeTotal.ToString() +" m³";

            });
        }

        /// <summary>
        /// window is closed by the "x" in the corner
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            _CancelTokenSource.Cancel();
            Log.LogManager.updateAlgorithmStatus("Done");
        }

        /// <summary>
        /// if the selection is changed in the listview the pictures have to be updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EuclidianListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                EuclidianListViewItem ecl = ((EuclidianListViewItem)e.AddedItems[0]);

                //transform
                List<List<Point>> points = Algorithm.DelaunayTriangulation.calculateDelaunayTrianglePoints(ecl.pointcloud);
                Post_knv_Server.DataIntegration.PointCloudDrawing.PointCloudPicturePackage pcpp = PointCloudDrawing.addTriangleToPicturePackage(refPointCloud.pictures, points);
                this._PointImageEuclid_Bottom.Source = pcpp.bottomview;
                this._PointImageEuclid_Front.Source = pcpp.frontview;
                this._PointImageEuclid_Side.Source = pcpp.sideview;
            }
            catch (Exception) { }
        }

        /// <summary>
        /// window close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonEuclidian_Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// not in use, only ok click relevant, has to be overwritten for the xaml
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CheckboxSelect_Unchecked(object sender, RoutedEventArgs e)
        {
            // do nothing
        }

        /// <summary>
        /// not in use, only ok click relevant, has to be overwritten for the xaml
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CheckboxSelect_Checked(object sender, RoutedEventArgs e)
        {
            // do nothing
        }

        /// <summary>
        /// The logic for the Ok button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonEuclidian_Ok_Click(object sender, RoutedEventArgs e)
        {
            // make the changes to the found clusters, if checked

            //create new 
            List<PointCloud> resultList = new List<PointCloud>();

            // for every found pointcloud add to list
            foreach (EuclidianListViewItem el in EuclidianListView.Items)
            {
                if (el.selectCluster)
                    resultList.Add(el.pointcloud);
            }

            //throw event if everything is in the list
            OnCloudListUpdatedEvent(resultList);

            this.Close();
        }

    }

    /// <summary>
    /// the class for the listviewItem for the euclidean window
    /// </summary>
    public class EuclidianListViewItem
    {
        public PointCloud pointcloud { get; set; }
        public int count { get; set; }
        public float cubicM { get; set; }
        public bool selectCluster { get; set; }
    }
}
