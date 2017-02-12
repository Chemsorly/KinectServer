using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post_KNV_MessageClasses
{
    /// <summary>
    /// object containing the kinect status at the moment of the request to ensure synchronisation
    /// </summary>
    [Serializable()]
    public class ClientRequestObject
    {
        /// <summary>
        /// is the kinect actually connected
        /// </summary>
        public bool isConnected { get; set; }

        /// <summary>
        /// is the kinect elidible for scan?
        /// </summary>
        public bool isMuted { get; set; }

        /// <summary>
        /// type of request
        /// </summary>
        public Post_KNV_MessageClasses.ClientConfigObject.RequestType requestType { get; set; }

        /// <summary>
        /// amounts of kinect in the request
        /// </summary>
        public int amountOfKinectsInRequest { get; set; }
    }
}
