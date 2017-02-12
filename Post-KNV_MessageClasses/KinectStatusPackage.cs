using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post_KNV_MessageClasses
{
    /// <summary>
    /// status package sent by the client to update the server with information about the kinect status
    /// </summary>
    [Serializable()]
    public class KinectStatusPackage
    {
        /// <summary>
        /// ID of the client
        /// </summary>
        public int clientID { get; set; }

        /// <summary>
        /// kinect status
        /// </summary>
        public bool isKinectActive { get; set; }
    }
}
