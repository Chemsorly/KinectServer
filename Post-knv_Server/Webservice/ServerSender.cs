using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Net;
using System.IO;
using Post_KNV_MessageClasses;
using System.Runtime.Serialization.Formatters.Binary;

namespace Post_knv_Server.Webservice
{
    /// <summary>
    /// this class represents the server sender, the task of the ServerSender
    /// is to handle external webservice communication
    /// </summary>
    public class ServerSender
    {
        
        #region external
        /// <summary>
        /// sends a scan request to a client
        /// </summary>
        /// <param name="pCco">the client config object which contains configuration parameters for the client</param>
        public void sendScanRequest(ClientConfigObject pCco)
        {
            // send the request
            sendRequestThread(@"http://" + pCco.ownIP + ":" +
                pCco.clientConnectionConfig.listeningPort + @"/SCAN", pCco, pCco, 10000);
        }

        /// <summary>
        /// sends the shutdown command to a client
        /// </summary>
        /// <param name="pCco">the client config object</param>
        public void sendShutdown(ClientConfigObject pCco)
        {
            Task<responseStruct> t = new Task<responseStruct>(() => sendRequestThread(
                 @"http://" + pCco.ownIP + ":" + pCco.clientConnectionConfig.listeningPort + @"/SHUTDOWN",
                 String.Empty,
                 pCco, 10000));
            // if the task fails
            t.ContinueWith(TaskFaultedHandler, TaskContinuationOptions.OnlyOnFaulted);
            t.Start();
        }

        /// <summary>
        /// sends a ping to a client
        /// </summary>
        /// <param name="pCco">the client config object</param>
        public void sendPing(ClientConfigObject pCco)
        {
            sendRequestThread(@"http://" + pCco.ownIP + ":" +
                pCco.clientConnectionConfig.listeningPort + @"/PING", "PING", pCco, 1000);
        }

        /// <summary>
        /// sends a new config to a client
        /// </summary>
        /// <param name="pCco">the config object</param>
        public void sendConfig(ClientConfigObject pCco)
        {
            // send the CCO
            Task<responseStruct> t = new Task<responseStruct>(() => sendRequestThread(
                @"http://" + pCco.ownIP + ":" + pCco.clientConnectionConfig.listeningPort + @"/CONFIG", 
                pCco, 
                pCco, 10000));

            // if the task fails
            t.ContinueWith(TaskFaultedHandler, TaskContinuationOptions.OnlyOnFaulted);
            t.Start();
        }

        #endregion

        #region internal
        /// <summary>
        /// gives the message of the exception to the LogManager
        /// </summary>
        /// <param name="pTask"></param>
        private void TaskFaultedHandler(Task pTask)
        {
            Log.LogManager.writeLog("[Webservice:ServerSender] ERROR: " + pTask.Exception.InnerException.Message);
        }

        
        /// <summary>
        /// task function to do the request
        /// </summary>
        /// <param name="URL">target</param>
        /// <param name="input">object to send to</param>
        /// <param name="pCco">identification object</param>
        /// <param name="pTimeout"timeout duration></param>
        /// <returns></returns>
        private responseStruct sendRequestThread(String URL, Object input, ClientConfigObject pCco, int pTimeout)
        {
            if (URL != null)
            {
                try
                {
                    // get the request stream
                    WebRequest request = WebRequest.Create(URL);                    
                    request.Method = "POST";
                    request.Timeout = pTimeout;
                    Stream requestStream = request.GetRequestStream();

                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(requestStream, input);
                    requestStream.Close();

                    // response to the request
                    WebResponse response = request.GetResponse();
                    StreamReader reader = new StreamReader(response.GetResponseStream());

                    responseStruct re = new responseStruct();
                    re.responseString = reader.ReadToEnd();
                    re.targetclient = pCco;
                    return re;
                }
                catch (Exception) { throw; }
            }

            responseStruct rel = new responseStruct();
            rel.responseString = "REQUEST NOT RECIEVED ERROR";
            rel.targetclient = pCco;
            return rel;
        }
        struct responseStruct { public string responseString; public ClientConfigObject targetclient;}
    }
        #endregion
}
