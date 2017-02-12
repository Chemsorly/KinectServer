using Post_KNV_MessageClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post_knv_Server.Log
{
    /// <summary>
    /// this class represents the LogManager, which has the task
    /// to give LogUpdates on several functions and statuses
    /// </summary>
    public static class LogManager
    {   
        /// <summary>
        /// writes a message into the UI log
        /// </summary>
        /// <param name="message">the log message</param>
        public delegate void OnLogMessageRecieved(String message);
        public static event OnLogMessageRecieved OnLogMessageEvent;
        public static void writeLog(String pMessage)
        {
            OnLogMessageEvent(pMessage);
        }

        /// <summary>
        /// writes a debugmessage into the ui log
        /// </summary>
        /// <param name="message">the debug message</param>
        public delegate void OnLogMessageDebugRecieved(String message);
        public static event OnLogMessageDebugRecieved OnLogMessageDebugEvent;
        public static void writeLogDebug(String pMessage)
        {
            OnLogMessageDebugEvent(pMessage);
        }

        /// <summary>
        /// updates the status changes on clients on the ui
        /// </summary>
        public delegate void OnClientStatusChangeRecieved();
        public static event OnClientStatusChangeRecieved OnClientStatusChangeEvent;
        public static void updateClientStatus()
        {
            OnClientStatusChangeEvent();
        }

        /// <summary>
        /// updates the status changes recieved from clients on the ui
        /// </summary>
        public delegate void OnClientKinectStatusChangeRecieved(KinectStatusPackage pKSP);
        public static event OnClientKinectStatusChangeRecieved OnClientKinectStatusChangeEvent;
        public static void updateClientStatus(KinectStatusPackage pKSP)
        {
            OnClientKinectStatusChangeEvent(pKSP);
        }
        
        /// <summary>
        /// updates the current used algorithm on the ui
        /// </summary>
        public delegate void OnServerStatusChange(String pStatus);
        public static event OnServerStatusChange OnServerStatusChangeEvent;

        public static void updateAlgorithmStatus(String pStatus)
        {
            OnServerStatusChangeEvent(pStatus);
        }

    }
}
