using Post_KNV_MessageClasses;
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
    /// Interaction logic for ScanResultWindow.xaml
    /// </summary>
    public partial class ScanResultWindow : Window
    {
        public ScanResultWindow(ScanResultPackage pResultPackage)
        {
            InitializeComponent();

            this._Textblock_Date.Text = pResultPackage.timestamp.ToString();
            this._Textblock_AlgorithmUsed.Text = pResultPackage.algorithmUsed.ToString();
            this._Textblock_NumberOfClouds.Text = pResultPackage.numberOfClouds.ToString();
            this._Textblock_NumberOfPoints.Text = pResultPackage.numberOfPoints.ToString();
            this._Textblock_ScannedDelaunayVolume.Text = Math.Round(pResultPackage.scannedDelaunayVolume,3).ToString() + " m³";
            this._Textblock_ConvexScannedAlgorithmVolume.Text = Math.Round(pResultPackage.convexScannedAlgorithmVolume,3).ToString() + " m³";
            this._Textblock_ConcavScannedAlgorithmVolume.Text = Math.Round(pResultPackage.concavScannedAlgorithmVolume,3).ToString() + " m³";
            this._Textblock_NumberOfContainers.Text = pResultPackage.containerResults.Sum(t => t.amount).ToString();
            this._Textblock_ContainerAccuracy.Text = Math.Round(pResultPackage.containerAccuracy,3).ToString() + "%";
            this._Textblock_PayloadVolume.Text = Math.Round(pResultPackage.estimatedPayloadVolume,3) + " m³";

            foreach(Post_KNV_MessageClasses.ScanResultPackage.ContainerResult res in pResultPackage.containerResults)
            {
                ContainerResultsListViewItem temp = new ContainerResultsListViewItem();
                temp.name = res.containerType.containerName;
                temp.amount = res.amount;
                _ContainerResultListView.Items.Add(temp);
            }
        }

        /// <summary>
        /// holder class for list view item
        /// </summary>
        class ContainerResultsListViewItem
        {
            /// <summary>
            /// name of the container
            /// </summary>
            public String name { get; set; }

            /// <summary>
            /// amount of container
            /// </summary>
            public int amount {get; set;}
        }
    }
}
