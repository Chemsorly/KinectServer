using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Diagnostics;
using System.Xml.Serialization;
using System.IO;
using Post_KNV_MessageClasses;
using System.Xml;
using Post_knv_Server.Log;
using System.ServiceModel.Description;

namespace Post_knv_Server.Webservice
{

    /// <summary>
    /// This class represents a listener, observing the clients
    /// </summary>
    public class ServerListener
    {
        // the listener uses a WebServiceHost to listen on the clients
        WebServiceHost _webserviceHost = null;
                
        /// <summary>
        /// This function implements the listener 
        /// </summary>
        /// <param name="port">the port to listen on</param>
        public ServerListener(int port)
        {
            try
            {
                // get the base address and a new Service   
                Uri baseAddress = new Uri(@"http://localhost:" + port + @"/");
                _webserviceHost = new WebServiceHost(typeof(ServerDefinition), baseAddress);
                
                // http binding
                WebHttpBinding binding = new WebHttpBinding();
                binding.MaxReceivedMessageSize = Int32.MaxValue;
                binding.SendTimeout = TimeSpan.MaxValue;
                                
                // add the service endpoint to the binding
                _webserviceHost.AddServiceEndpoint(typeof(WebserviceContract), binding,"");

                // open the connection
                _webserviceHost.Open();
                
                // give feedback to the LogManager
                LogManager.writeLog("[Webservice:ServerListener] succesfully started");
            }
            catch (Exception ex)
            {
                // give feedback to the LogManager
                LogManager.writeLog("[Webservice:ServerListener] ERROR: " + ex.Message);
            }
        }

        /// <summary>
        /// destructor
        /// </summary>
        ~ServerListener()
        { this.Close(); }

        /// <summary>
        /// disposes the current ServiceListener before opening a new one
        /// </summary>
        public void Close()
        {
            try
            {
                if (_webserviceHost != null)
                {
                    _webserviceHost.Close();
                    _webserviceHost = null;
                }
            }
            catch (Exception) { }
        }
    }
}
