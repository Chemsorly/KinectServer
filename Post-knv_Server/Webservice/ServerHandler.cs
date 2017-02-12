using Post_KNV_MessageClasses;
using Post_knv_Server.Registrationservice;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post_knv_Server.Webservice
{
    /// <summary>
    /// this class represents the server handler, which has the task
    /// to handle internal webservice communictaion
    /// </summary>
    class ServerHandler
    {

        #region fields

        /// <summary>
        /// the listener
        /// </summary>
        private ServerListener _WebserviceListener;

        /// <summary>
        /// the sender
        /// </summary>
        private ServerSender _WebserviceSender;

        /// <summary>
        /// the registration service
        /// </summary>
        private RegistrationService _RegService;

        /// <summary>
        /// the signalr handler
        /// </summary>
        private SignalRHandler _SignalRHandler;

        #endregion

        #region external

        //Event definitions
        public delegate void OnKinectDataSend(KinectDataPackage EventArgs);
        public event OnKinectDataSend OnKinectDataPackageSendEvent;

        //event Apprequest from application or website
        public delegate String OnAppScanrequestRecieved(AppRequestObject EventArgs);
        public event OnAppScanrequestRecieved OnAppScanrequestEvent;
        
        /// <summary>
        /// the communication between the DataManager and the ServerHandler
        /// </summary>
        /// <param name="pDBManager"></param>
        public ServerHandler(DataManager.DBManager pDBManager)
        {
            _WebserviceListener = new ServerListener(Config.ServerConfigManager._ServerConfigObject.listeningPort);
            
            _WebserviceSender = new ServerSender();

            _RegService = new RegistrationService(pDBManager);
            _RegService.OnPingRequestSendingEvent += _RegService_OnPingRequestSendingEvent;
            _RegService.OnConfigRequestSendingEvent += sendConfigToClient;

            if(Config.ServerConfigManager._ServerConfigObject.signalrAddress != String.Empty)
            {
                _SignalRHandler = new SignalRHandler();
                _SignalRHandler.Initialize(@"http://" + Config.ServerConfigManager._ServerConfigObject.signalrAddress + @"/signalr");
            }
          
            ServerDefinition.OnHelloRequestEvent += WebserviceContract_OnHelloRequestEvent;
            ServerDefinition.OnConfigRequestEvent += ServerWebserviceContract_OnConfigRequestEvent;
            ServerDefinition.OnKinectDataPackageEvent += ServerWebserviceContract_OnKinectDataPackageEvent;
            ServerDefinition.OnKinectStatusPackageEvent += ServerWebserviceContract_OnKinectStatusPackageEvent;
            ServerDefinition.OnAppScanrequestEvent += ServerDefinition_OnAppScanrequestEvent;
        }

        /// <summary>
        /// gets called when a mobile app requests a scan
        /// </summary>
        /// <param name="EventArgs">app request object for remote requesting</param>
        /// <returns>a status string if request was successful or not</returns>
        String ServerDefinition_OnAppScanrequestEvent(AppRequestObject EventArgs)
        { return OnAppScanrequestEvent(EventArgs); }            

        /// <summary>
        /// gets fired whenever a status package was recieved
        /// </summary>
        /// <param name="EventArgs">the status package</param>
        void ServerWebserviceContract_OnKinectStatusPackageEvent(KinectStatusPackage EventArgs)
        { Log.LogManager.updateClientStatus(EventArgs); }

        /// <summary>
        /// kills the webservice so a new instance can be created
        /// </summary>
        public void killWebservice()
        {
            this._WebserviceListener.Close();
        }

        /// <summary>
        /// the service contract HelloRequest
        /// </summary>
        /// <param name="EventArgs">the HRO</param>
        public void WebserviceContract_OnHelloRequestEvent(Post_KNV_MessageClasses.HelloRequestObject EventArgs)
        {
            _RegService.recieveHelloRequest(EventArgs);
        }

        /// <summary>
        /// the shutdown call for the client
        /// </summary>
        /// <param name="pCco">the CCO</param>
        public void sendShutdownToClient(ClientConfigObject pCco)
        {
            _WebserviceSender.sendShutdown(pCco);
        }

        /// <summary>
        /// the transport of the config to the client
        /// </summary>
        /// <param name="pCco">the CCO</param>
        public void sendConfigToClient(ClientConfigObject pCco)
        {
            _WebserviceSender.sendConfig(pCco);
        }

        /// <summary>
        /// the scan request for the clients
        /// </summary>
        /// <param name="pCco">the CCO</param>
        public void sendScanRequestToClient(ClientConfigObject pCco)
        {
            _WebserviceSender.sendScanRequest(pCco);
        }

        /// <summary>
        /// sends a scan result to all connected clients
        /// </summary>
        /// <param name="pResult">the result package</param>
        public void sendScanResults(ScanResultPackage pResult)
        {
            if(_SignalRHandler != null) _SignalRHandler.SendScanResultToClients(pResult);
        }

        #endregion

        #region internal

        /// <summary>
        /// throw the event that tells the data manager that new kinect data was recieved
        /// </summary>
        /// <param name="EventArgs"></param>
        private void ServerWebserviceContract_OnKinectDataPackageEvent(KinectDataPackage EventArgs)
        {
            OnKinectDataPackageSendEvent.BeginInvoke(EventArgs,null,null);
        }

        /// <summary>
        /// the webservice contract for the config objects
        /// </summary>
        /// <param name="EventArgs">the CCO</param>
        void ServerWebserviceContract_OnConfigRequestEvent(ClientConfigObject EventArgs)
        {
            _RegService.receiveConfigObject(EventArgs);
        }

        /// <summary>
        /// the regservice for the ping request event
        /// </summary>
        /// <param name="targetClient">the CCO</param>
        void _RegService_OnPingRequestSendingEvent(ClientConfigObject targetClient)
        {
            try { 
                _WebserviceSender.sendPing(targetClient); 
            }
            catch (Exception) { 
                _RegService.removeClientFromList(targetClient);
            }
        }

        #endregion
    }
}
