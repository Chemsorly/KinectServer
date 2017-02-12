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
    /// Interaction logic for DebugTransformationMatrix.xaml
    /// </summary>
    public partial class DebugTransformationMatrix : Window
    {
        public double[,] transformationMatrix { get; set; }

        public DebugTransformationMatrix()
        {
            InitializeComponent();
            transformationMatrix = new double[4, 4];
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Transformation matrix
            transformationMatrix[0, 0] = double.Parse(this._Txtbox_Trans_00.Text);
            transformationMatrix[1, 0] = double.Parse(this._Txtbox_Trans_10.Text);
            transformationMatrix[2, 0] = double.Parse(this._Txtbox_Trans_20.Text);
            transformationMatrix[3, 0] = double.Parse(this._Txtbox_Trans_30.Text);

            transformationMatrix[0, 1] = double.Parse(this._Txtbox_Trans_01.Text);
            transformationMatrix[1, 1] = double.Parse(this._Txtbox_Trans_11.Text);
            transformationMatrix[2, 1] = double.Parse(this._Txtbox_Trans_21.Text);
            transformationMatrix[3, 1] = double.Parse(this._Txtbox_Trans_31.Text);

            transformationMatrix[0, 2] = double.Parse(this._Txtbox_Trans_02.Text);
            transformationMatrix[1, 2] = double.Parse(this._Txtbox_Trans_12.Text);
            transformationMatrix[2, 2] = double.Parse(this._Txtbox_Trans_22.Text);
            transformationMatrix[3, 2] = double.Parse(this._Txtbox_Trans_32.Text);

            transformationMatrix[0, 3] = double.Parse(this._Txtbox_Trans_03.Text);
            transformationMatrix[1, 3] = double.Parse(this._Txtbox_Trans_13.Text);
            transformationMatrix[2, 3] = double.Parse(this._Txtbox_Trans_23.Text);
            transformationMatrix[3, 3] = double.Parse(this._Txtbox_Trans_33.Text);
        }
    }
}
