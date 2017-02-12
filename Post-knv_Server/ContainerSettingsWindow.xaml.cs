using Post_knv_Server.Config;
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
    /// Interaction logic for ContainerSettingsWindow.xaml
    /// </summary>
    public partial class ContainerSettingsWindow : Window
    {
        /// <summary>
        /// field that contains the calibrated containers
        /// </summary>
        public List<ContainerConfigObject> containerList;

        /// <summary>
        /// constructor, takes the container list and writes it in the listview
        /// </summary>
        /// <param name="pContainerList">the container list</param>
        public ContainerSettingsWindow(List<ContainerConfigObject> pContainerList)
        {
            InitializeComponent();
            containerList = new List<ContainerConfigObject>();

            //put incoming list into listview
            foreach(ContainerConfigObject container in pContainerList)
            {
                //create new list view item
                ContainerListviewItem item = new ContainerListviewItem();
                item._container = container;
                item.name = container.containerName;
                item.height = container.containerHeight;
                item.depth = container.containerDepth;
                item.width = container.containerWidth;
                item.volume = container.containerVolume;

                //add list view item
                _ListView_Containers.Items.Add(item);
            }
        }

        /// <summary>
        /// gets fired when the add button gets pressed, tries to add a container to the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonAddContainer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //create new config item
                ContainerConfigObject cco = new ContainerConfigObject();
                cco.containerName = this._TextBox_ContainerName.Text;
                cco.containerHeight = double.Parse(this._TextBox_ContainerHeight.Text);
                cco.containerWidth = double.Parse(this._TextBox_ContainerWidth.Text);
                cco.containerDepth = double.Parse(this._TextBox_ContainerDepth.Text);
                cco.containerVolume = double.Parse(this._TextBox_ContainerVolume.Text);

                //add to listview
                ContainerListviewItem item = new ContainerListviewItem();
                item._container = cco;
                item.name = cco.containerName;
                item.height = cco.containerHeight;
                item.depth = cco.containerDepth;
                item.width = cco.containerWidth;
                item.volume = cco.containerVolume;

                //add list view item
                _ListView_Containers.Items.Add(item);

                //clear fields
                this._TextBox_ContainerName.Text = String.Empty;
                this._TextBox_ContainerHeight.Text = String.Empty;
                this._TextBox_ContainerWidth.Text = String.Empty;
                this._TextBox_ContainerDepth.Text = String.Empty;
                this._TextBox_ContainerVolume.Text = String.Empty;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        /// <summary>
        /// gets fired when the remove button gets pressed, removes selected items from the list view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Button_RemoveContainer_Click(object sender, RoutedEventArgs e)
        {
            //fetch selected items
            List<ContainerListviewItem> fetchedItems = new List<ContainerListviewItem>();
            foreach (ContainerListviewItem item in _ListView_Containers.SelectedItems)
                fetchedItems.Add(item);

            //delete fetched items
            foreach (ContainerListviewItem item in fetchedItems)
                this._ListView_Containers.Items.Remove(item);
        }

        /// <summary>
        /// gets fired when the ok button is pressed. sets the result trie and sets the container field so it can be accessed from the calling thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Button_Ok_Click(object sender, RoutedEventArgs e)
        {
            foreach(ContainerListviewItem item in _ListView_Containers.Items)
                containerList.Add(item._container);

            this.DialogResult = true;
        }

        /// <summary>
        /// closes the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// holding class for the list view
        /// </summary>
        class ContainerListviewItem
        {
            public ContainerConfigObject _container { get; set; }
            public String name { get; set; }
            public double height { get; set; }
            public double width { get; set; }
            public double depth { get; set; }
            public double volume { get; set; }
        }
    }
}
