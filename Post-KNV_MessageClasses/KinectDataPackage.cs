using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Post_KNV_MessageClasses
{
    /// <summary>
    /// data package sent by the clients to the server. contains depth data and CCO for identification
    /// </summary>
    [Serializable()]
    public class KinectDataPackage
    {

        /// <summary>
        /// the CCO. used for identification
        /// </summary>
        public ClientConfigObject usedConfig { get; set; }

        /// <summary>
        /// the depth data
        /// </summary>
        public List<ushort[]> rawDepthData { get; set; }
        
        /// <summary>
        /// constructor for serialization
        /// </summary>
        public KinectDataPackage() { }

        /// <summary>
        /// constructor; a ClientConfigObject is necessary
        /// </summary>
        /// <param name="pCco">the client config object</param>
        public KinectDataPackage(ClientConfigObject pCco)
        {
            usedConfig = pCco;
            rawDepthData = new List<ushort[]>();
        }

        /// <summary>
        /// adds data to the data array
        /// </summary>
        /// <param name="pInputData">the depth data array</param>
        public void addData(ushort[] pInputData)
        {
            rawDepthData.Add(pInputData);
        }
    }
}
