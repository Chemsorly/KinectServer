using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Post_KNV_MessageClasses
{
    [Serializable()]
    public class ClientConfigObject
    {
        public String ownIP { get; set; }
        public int ID { get; set; }
        public String name { get; set; }
        public bool debug { get; set; }
        public ClientRequestObject clientRequestObject { get; set; }
        public ClientConnectionConfigObject clientConnectionConfig { get; set; }
        public ClientKinectConfigObject clientKinectConfig { get; set; }

        /// <summary>
        /// creates a default configured ClientConfigObject
        /// </summary>
        /// <returns>the default CCO</returns>
        public static ClientConfigObject createDefaultConfig()
        {
            return new ClientConfigObject()
            {
                ID = -1,
                name = "client_default",
                ownIP = getOwnIP(),
                debug = true,
                clientKinectConfig = new ClientKinectConfigObject()
                {
                    numberOfPictures = 100,
                    minDepth = 0,
                    maxDepth = 10000,
                    xMinDepth = 0,
                    xMaxDepth = 512,
                    yMinDepth = 0,
                    yMaxDepth = 424,
                    isMaster = false,
                    transformationMatrix = getIdentityMatrix()
                },
                clientConnectionConfig = new ClientConnectionConfigObject()
                {
                    keepAliveInterval = 10000,
                    targetIP = getOwnIP() + ":8999", //ToDo: change target ip to GATEWAY
                    targetGateway = String.Empty,
                    listeningPort = 8998
                },
                clientRequestObject = new ClientRequestObject()
                {
                    isConnected = true,
                    isMuted = false,
                    amountOfKinectsInRequest = 0,
                    requestType = RequestType.fetch
                }
            };
        }

        /// <summary>
        /// creates a 4x4 identity matrix and returns it
        /// </summary>
        /// <returns>the identity matrix</returns>
        static double[,] getIdentityMatrix()
        {
            double[,] matrix = new double[4, 4];
            matrix[0, 0] = 1; matrix[1, 1] = 1;
            matrix[2, 2] = 1; matrix[3, 3] = 1;
            return matrix;
        }

        /// <summary>
        /// gets the own current IPv4
        /// </summary>
        /// <returns>the own IPv4</returns>
        public static String getOwnIP()
        {
            string localIP = "localhost";
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                IPHostEntry host;
                host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                        break;
                    }
                }
            }
            return localIP;
        }

        /// <summary>
        /// the different types of request
        /// </summary>
        public enum RequestType { calibration, fetch, scan };
    }
}
