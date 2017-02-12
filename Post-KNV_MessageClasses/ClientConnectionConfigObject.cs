using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post_KNV_MessageClasses
{
    /// <summary>
    /// the client connection object. tells the client where to send data and where to listen to
    /// </summary>
    [Serializable()]
    public class ClientConnectionConfigObject
    {
        /// <summary>
        /// the target IP of the server
        /// </summary>
        public String targetIP { get; set; }

        /// <summary>
        /// the target gateway, if configured
        /// </summary>
        public String targetGateway { get; set; }

        /// <summary>
        /// the listening port
        /// </summary>
        public int listeningPort { get; set; }

        /// <summary>
        /// the interval to recieve PING and PONGs
        /// </summary>
        public double keepAliveInterval { get; set; }
    }
}
