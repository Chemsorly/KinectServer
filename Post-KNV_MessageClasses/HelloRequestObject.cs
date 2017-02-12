using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post_KNV_MessageClasses
{
    /// <summary>
    /// Hello Request Object sent from the client to the server to register at the server. Server either answers with a fresh CCO or a saved CCO based on the ID
    /// </summary>
    [Serializable()]
    public class HelloRequestObject
    {
        /// <summary>
        /// IP of the client to send the answer to
        /// </summary>
        public String ownIP;

        /// <summary>
        /// the ID, if applicable
        /// </summary>
        public int ID;

        /// <summary>
        /// the name
        /// </summary>
        public String Name;

        /// <summary>
        /// creates a HelloRequestObject based on the current configuration
        /// </summary>
        /// <param name="pInputConfig">the input config</param>
        /// <returns>the HelloRequestObject</returns>
        public static HelloRequestObject createHelloFromClientConfig(ClientConfigObject pInputConfig)
        {
            return new HelloRequestObject
            {
                ownIP = pInputConfig.ownIP,
                ID = pInputConfig.ID,
                Name = pInputConfig.name
            };
        }
    }
}
