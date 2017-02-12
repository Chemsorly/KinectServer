using Microsoft.AspNet.SignalR.Client;
using Post_KNV_MessageClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post_knv_Server.Webservice
{
    /// <summary>
    /// class to manage the SignalR connection
    /// </summary>
    class SignalRHandler
    {
        /// <summary>
        /// the hub connection
        /// </summary>
        HubConnection hubConnection;

        /// <summary>
        /// the hub proxy to communicate with the signalR server
        /// </summary>
        IHubProxy HubProxy;

        /// <summary>
        /// the server URL
        /// </summary>
        String serverURI;

        /// <summary>
        /// the name to use upon connection
        /// </summary>
        const string _name = "KinectServer";

        /// <summary>
        /// constructor
        /// </summary>
        public SignalRHandler()
        {
            Log.LogManager.OnServerStatusChangeEvent += LogManager_OnAlgorithmStatusChangeEvent;
        }

        /// <summary>
        /// event handler to send a message to the server when the algorithm status changes
        /// </summary>
        /// <param name="pStatus"></param>
        void LogManager_OnAlgorithmStatusChangeEvent(String pStatus)
        {
            if (HubProxy != null)
                try
                {
                    HubProxy.Invoke("SendStatusMessage", _name, pStatus);
                }
                catch (Exception) { }
        }

        /// <summary>
        /// initializes the signalR handler by connecting to the signalr server
        /// </summary>
        /// <param name="pServerURI"></param>
        public void Initialize(String pServerURI)
        {
            this.serverURI = pServerURI;
            Connect(this.serverURI);
        }

        /// <summary>
        /// sends a scan result package to the signalr server which then broadcasts it to all clients
        /// </summary>
        /// <param name="pResult"></param>
        public void SendScanResultToClients(ScanResultPackage pResult)
        {
            try
            {
                HubProxy.Invoke("SendScanResult", _name, pResult);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// connects to the signalr server
        /// </summary>
        /// <param name="pServerURI">server to connect to</param>
        private async void Connect(String pServerURI)
        {
            try
            {
                if (this.hubConnection != null) this.hubConnection.Closed -= Connection_Closed;
            }catch (Exception) { }

            this.hubConnection = new HubConnection(pServerURI);
            this.hubConnection.Closed += Connection_Closed;
            this.HubProxy = hubConnection.CreateHubProxy("ChatHub");
            HubProxy.On<string, string>("onConnected", (id, userName) => Log.LogManager.writeLog("[SignalR] Connected as " + userName));

            try
            {
                await hubConnection.Start();
            }catch(Exception ex)
            {
                Log.LogManager.writeLogDebug("[SignalR] ERROR: " + ex.Message);
                return;
            }

            await HubProxy.Invoke("Connect", _name);
        }

        /// <summary>
        /// If the server is stopped, the connection will time out after 30 seconds (default), and the closed event will fire.
        /// </summary>
        void Connection_Closed()
        {
            Connect(this.serverURI);
        }

    }
}
