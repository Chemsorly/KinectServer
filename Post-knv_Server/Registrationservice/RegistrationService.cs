using Post_KNV_MessageClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Post_knv_Server.Registrationservice
{
    /// <summary>
    /// this class implements all the functions needed for the clients to register with
    /// the server
    /// </summary>
    public class RegistrationService
    {
        #region fields

        //DataBase Manager for the registration process
        DataManager.DBManager _DBManager;

        //the timer that triggers the ping events
        Timer _pingTimer;

        #endregion

        #region external

        // event: PingRequestSending
        public delegate void OnPingRequestSending(ClientConfigObject targetClient);
        public event OnPingRequestSending OnPingRequestSendingEvent;

        // event: ConfigRequestSending
        public delegate void OnConfigRequestSending(ClientConfigObject targetClient);
        public event OnConfigRequestSending OnConfigRequestSendingEvent;

        /// <summary>
        /// the service procedure for the registration
        /// </summary>
        /// <param name="pDBManager">the DBmanager to be forwarded</param>
        public RegistrationService(DataManager.DBManager pDBManager)
        {
            _DBManager = pDBManager;

            // if the client fires an update event
            _DBManager.OnClientUpdatedEvent += _DBManager_OnClientUpdatedEvent;

            // set and use the ping timer
            _pingTimer = new Timer(Config.ServerConfigManager._ServerConfigObject.keepAliveInterval);
            _pingTimer.Elapsed += _pingTimer_Elapsed;
            _pingTimer.AutoReset = true;
            _pingTimer.Start();
        }

        /// <summary>
        /// removes a client from the database
        /// </summary>
        /// <param name="pCco">the CCO</param>
        public void removeClientFromList(ClientConfigObject pCco)
        {
            if (pCco.clientRequestObject.isConnected)
            {
                Log.LogManager.writeLog("[Registrationservice:RegistrationService] Client " + pCco.name + " disconnected.");
                pCco.clientRequestObject.isConnected = false;
                _DBManager.updateClient(pCco, false);
            }
        }

        /// <summary>
        /// new config recieved from client
        /// </summary>
        /// <param name="pCco">the CCO</param>
        public void receiveConfigObject(ClientConfigObject pCco)
        {
            Log.LogManager.writeLog("[RegistrationService:RegistrationService] New configuration recieved from " + pCco.name);
            _DBManager.updateClient(pCco, true);
        }

        /// <summary>
        /// recieve a HelloRequest from the client
        /// </summary>
        /// <param name="pHro">the HRO</param>
        public void recieveHelloRequest(HelloRequestObject pHro)
        {
            Log.LogManager.writeLogDebug("[RegistrationService:RegistrationService] Hello request recieved from " + pHro.Name + " with the ID " + pHro.ID);
            ClientConfigObject cco = _DBManager.recieveHelloRequest(pHro);
            Log.LogManager.writeLogDebug("[RegistrationService:RegistrationService] Hello: CCO generated, isConnected: " + cco.clientRequestObject.isConnected);
            OnConfigRequestSendingEvent(cco);
        }

        #endregion

        #region internal

        /// <summary>
        /// give the DBManager the ConfigRequestSend event
        /// </summary>
        /// <param name="pCco">the CCO</param>
        void _DBManager_OnClientUpdatedEvent(ClientConfigObject pCco)
        {
            OnConfigRequestSendingEvent(pCco);
        }

        /// <summary>
        /// this function pings all clients in an timeinterval
        /// </summary>
        void _pingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            List<ClientConfigObject> _kinectClientList = _DBManager.recieveClientList();
            foreach (ClientConfigObject cco in _kinectClientList)
            {
                if (cco.clientRequestObject.isConnected)
                    OnPingRequestSendingEvent.BeginInvoke(cco,null,null);
            }
        }

        #endregion
    }
}
