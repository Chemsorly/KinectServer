using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post_KNV_MessageClasses
{
    /// <summary>
    /// container calibration object that contains the container variables
    /// </summary>
    [Serializable]
    public class ContainerConfigObject
    {
        /// <summary>
        /// the name/identification of the container
        /// </summary>
        public String containerName { get; set; }

        /// <summary>
        /// the width of the container
        /// </summary>
        public double containerWidth { get; set; }

        /// <summary>
        /// the height of the container
        /// </summary>
        public double containerHeight { get; set; }

        /// <summary>
        /// the depth of the container
        /// </summary>
        public double containerDepth { get; set; }
        
        /// <summary>
        /// the empty volume of the container
        /// </summary>
        public double containerVolume { get; set; }

    }
}
