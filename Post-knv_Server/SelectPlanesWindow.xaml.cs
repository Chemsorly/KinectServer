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
    /// interaction logic for SelectPlanes.xaml
    /// </summary>
    public partial class SelectPlanesWindow : Window
    {
        PointCloud pointCloud;

        List<PlaneModel> refList;
        List<PlaneModel> planeModelList;
        PlaneModel floorPlane;

        DataManager.DataManager _DataManager;

        System.Threading.CancellationTokenSource _CancelTokenSource;
        
        /// <summary>
        /// event to throw when the plane list has been updated
        /// </summary>
        /// <param name="pl">the updated plane list to be broadcasted</param>
        internal delegate void OnPlaneListUpdatedReady(List<PlaneModel> pl);
        internal event OnPlaneListUpdatedReady OnPlaneListUpdatedEvent;
        
        /// <summary>
        /// constructor of the select planes window
        /// </summary>
        /// <param name="pc">the current reference cloud</param>
        /// <param name="pm">the plane models</param>
        public SelectPlanesWindow(PointCloud pc, List<PlaneModel> pm, DataManager.DataManager dm)
        {            
            InitializeComponent();
            _CancelTokenSource = new System.Threading.CancellationTokenSource();
            pointCloud = pc;
            planeModelList = new List<PlaneModel>();
            foreach (PlaneModel pl in pm)
            {
                planeModelList.Add(pl);
                if (pl.isFloor)
                    floorPlane = pl;
            }
            
            refList = new List<PlaneModel>();
            foreach (PlaneModel refpl in planeModelList) refList.Add(refpl);

            _DataManager = dm;

            UpdateListView();
        }

        /// <summary>
        /// update function for the listview with the plane models found by the algorithm
        /// </summary>
        private void UpdateListView()
        {
            this.Dispatcher.Invoke(() =>
            {
                SelectPlanesListView.Items.Clear();
                //add new items to the listview
                if (planeModelList != null)
                {
                    foreach (PlaneModel pl in planeModelList)
                    {
                        bool origin = false;
                        if (refList.Exists(t => t.Equals(pl))) origin = true;

                        bool master = false;
                        if (pl == floorPlane) master = true;

                        SelectPlanesListViewItem splv = new SelectPlanesListViewItem() { PlaneModel = pl, inlier = pl.inliers, D = pl.anxPlane.D, N = pl.anxPlane.Normal, selectPlane = origin, selectMaster = master };
                        SelectPlanesListView.Items.Add(splv);
                    }
                    
                }
            });
        }        
        
        /// <summary>
        /// the functions that get called when the user clicks the ok button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonSelectPlanes_Ok_Click(object sender, RoutedEventArgs e)
        {
            //create new 
            List<PlaneModel> resultList = new List<PlaneModel>();

            foreach(SelectPlanesListViewItem sl in SelectPlanesListView.Items)
            {
                if (sl.selectPlane)
                    resultList.Add(sl.PlaneModel);
            }

            foreach (PlaneModel pl in planeModelList) pl.isFloor = false;
             if(floorPlane != null) floorPlane.isFloor = true;

            //throw event
            OnPlaneListUpdatedEvent(resultList);
            this.Close();
        }

        /// <summary>
        /// cancel the things done in the select planes window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonSelectPlanes_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// user switches the plane model in the listview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectPlanesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                SelectPlanesListViewItem selectedItem = ((SelectPlanesListViewItem)e.AddedItems[0]);

                //transform
                List<List<Point>> points = new List<List<Point>>();
                points.Add(selectedItem.PlaneModel.getPointList());

                Post_knv_Server.DataIntegration.PointCloudDrawing.PointCloudPicturePackage pcpp = PointCloudDrawing.addTriangleToPicturePackage(pointCloud.pictures, points);
                this._PointImagePlane_Bottom.Source = pcpp.bottomview;
                this._PointImagePlane_Front.Source = pcpp.frontview;
                this._PointImagePlane_Side.Source = pcpp.sideview;
            }
            catch (Exception) { }
        }

        //the logic for the automatic search button (find new planes)
        Task t;
        private void _ButtonFindNewPlanes_Click(object sender, RoutedEventArgs e)
        {
            //check if still running
            if (t != null && t.Status == TaskStatus.Running) return;

            //check if input parameters are correct
            int amountplanes;
            try{ amountplanes = int.Parse(this._TextboxNumberOfPlanes.Text);  }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error); return; }

            t = new Task(() =>
            {
                try
                {
                    //check for planes
                    List<Post_knv_Server.DataIntegration.PlaneModel> planes = _DataManager.getPlanes(amountplanes, _CancelTokenSource.Token);
                    if (planes == null || planes.Count < 1) throw new Exception("Plane list null or empty");
                    foreach (PlaneModel pl in planes) this.Dispatcher.Invoke(() => planeModelList.Add(pl));
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error); }
            }, _CancelTokenSource.Token);
            t.ContinueWith(PlanesTaskHandler, TaskContinuationOptions.OnlyOnRanToCompletion);
            t.Start();
        }

        /// <summary>
        /// task handler
        /// </summary>
        /// <param name="pTask">task</param>
        void PlanesTaskHandler(Task pTask)
        {
            UpdateListView();
            Log.LogManager.updateAlgorithmStatus("Done");
        }

        /// <summary>
        /// manually adds a plane to the calibration based on the input vectors
        /// </summary>
        private void _ButtonAddPlaneManually_Click(object sender, RoutedEventArgs e)
        {
            try 
            {
                //fetch values
                ANX.Framework.Vector3 V1 = new ANX.Framework.Vector3(float.Parse(_TextboxManualPlaneV1X.Text), float.Parse(_TextboxManualPlaneV1Y.Text), float.Parse(_TextboxManualPlaneV1Z.Text));
                ANX.Framework.Vector3 V2 = new ANX.Framework.Vector3(float.Parse(_TextboxManualPlaneV2X.Text), float.Parse(_TextboxManualPlaneV2Y.Text), float.Parse(_TextboxManualPlaneV2Z.Text));
                ANX.Framework.Vector3 V3 = new ANX.Framework.Vector3(float.Parse(_TextboxManualPlaneV3X.Text), float.Parse(_TextboxManualPlaneV3Y.Text), float.Parse(_TextboxManualPlaneV3Z.Text));

                //create plane
                PlaneModel newPlane = new PlaneModel(V1, V2, V3);
                newPlane.calculateInliers(pointCloud, Config.ServerConfigManager._ServerConfigObject.serverAlgorithmConfig.planar_ThresholdDistance);
                
                //add plane to list
                planeModelList.Add(newPlane);
                UpdateListView();

                //clear fields
                this._TextboxManualPlaneV1X.Text = string.Empty; this._TextboxManualPlaneV1Y.Text = string.Empty; this._TextboxManualPlaneV1Z.Text = string.Empty;
                this._TextboxManualPlaneV2X.Text = string.Empty; this._TextboxManualPlaneV2Y.Text = string.Empty; this._TextboxManualPlaneV2Z.Text = string.Empty;
                this._TextboxManualPlaneV3X.Text = string.Empty; this._TextboxManualPlaneV3Y.Text = string.Empty; this._TextboxManualPlaneV3Z.Text = string.Empty;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        /// <summary>
        /// gets called when the window is closed (to ensure, the current algorithms get canceled)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            _CancelTokenSource.Cancel();
            Log.LogManager.updateAlgorithmStatus("Done");
        }

        /// <summary>
        /// sets or unsets a the floor plane
        /// </summary>
        private void _CheckboxFloorSelect_Checked(object sender, RoutedEventArgs e)
        {
            SelectPlanesListViewItem obj = ((FrameworkElement)sender).DataContext as SelectPlanesListViewItem;
            floorPlane = planeModelList.Find(pl => pl == obj.PlaneModel);

            UpdateListView();
        }
    }

    // class for the select planes listview item
    class SelectPlanesListViewItem
    {
        public PlaneModel PlaneModel { get; set; }
        public int inlier { get; set; }
        public ANX.Framework.Vector3 N { get; set; }
        public float D { get; set; }
        public bool selectPlane { get; set; }
        public bool selectMaster { get; set; }
    }
}
