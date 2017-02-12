using Post_KNV_MessageClasses;
using Post_knv_Server.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Post_knv_Server.Webservice
{

    /// <summary>
    /// this interface defines the service contracts needed for the 
    /// client server communication
    /// </summary>
    [ServiceContract]
    public interface WebserviceContract
    {
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "HELLO")]
        String responseHello(Stream message);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "CONFIG")]
        String responseConfig(Stream message);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "KINECTDATA")]
        String responseKinectData(Stream message);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "KINECTSTATUS")]
        String responseKinectStatus(Stream message);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "APPREQUEST", ResponseFormat = WebMessageFormat.Json)]
        String responseAppRequest(Stream message);
    }

    /// <summary>
    /// this class defines the server and it's contracts
    /// </summary>
    public class ServerDefinition : WebserviceContract
    {
        /// <summary>
        /// create events
        /// </summary>
        /// <param name="EventArgs"></param>
        #region external
        
        // event: HelloRequestRecieved from client
        public delegate void OnHelloRequestRecieved(HelloRequestObject EventArgs);
        public static event OnHelloRequestRecieved OnHelloRequestEvent;

        // event: ConfigRequestRecieved from client
        public delegate void OnConfigRequestRecieved(ClientConfigObject EventArgs);
        public static event OnConfigRequestRecieved OnConfigRequestEvent;

        // event: KinectDataRecieved from client
        public delegate void OnKinectDataRecieved(KinectDataPackage EventArgs);
        public static event OnKinectDataRecieved OnKinectDataPackageEvent;
        #endregion

        //event KinectStatusRecieved from client
        public delegate void OnKinectStatusRecieved(KinectStatusPackage EventArgs);
        public static event OnKinectStatusRecieved OnKinectStatusPackageEvent;

        //event Apprequest from application or website
        public delegate String OnAppScanrequestRecieved(AppRequestObject EventArgs);
        public static event OnAppScanrequestRecieved OnAppScanrequestEvent;

        #region internal
               
        /// <summary>
        /// response function from a hello. 
        /// </summary>
        /// <param name="message">the stream recieved from the client</param>
        /// <returns>a status message</returns>
        public String responseHello(Stream message)
        {
            // deserialize
            BinaryFormatter formatter = new BinaryFormatter();
            HelloRequestObject hro = (HelloRequestObject)formatter.Deserialize(message);

            // give info in the Log
            LogManager.writeLog("[Webservice:ServerDefinition] Client " + hro.Name.ToString() + " " + hro.ownIP.ToString() + " has connected.");

            // fire the event
            OnHelloRequestEvent(hro);

            // feedback for the client
            return "HELLOREQUEST RECIEVED";
        }

        /// <summary>
        /// gets fired when the server recieves a config request from the client
        /// </summary>
        /// <param name="message">the stream</param>
        /// <returns>a status message</returns>
        public String responseConfig(Stream message)
        {
            // deserialize
            BinaryFormatter formatter = new BinaryFormatter();
            ClientConfigObject cco = (ClientConfigObject)formatter.Deserialize(message);

            // fire the event
            OnConfigRequestEvent.BeginInvoke(cco, null, null);

            // feedback for the client
            return "CONFIG RECIEVED";
        }

        /// <summary>
        /// gets fired when the server recieves a kinect data package from the client
        /// </summary>
        /// <param name="input">input stream</param>
        /// <returns>a status message</returns>
        public String responseKinectData(Stream input) 
        {
            // make the input stream an object
            BinaryFormatter formatter = new BinaryFormatter();
            KinectDataPackage KdP = (KinectDataPackage)formatter.Deserialize(input);
            LogManager.writeLogDebug("[Webservice:ServerDefinition] Kinect Data Package recieved from ClientID " + KdP.usedConfig.ID);
            OnKinectDataPackageEvent(KdP);

            return "KINECT DATA RECIEVED";
        }

        /// <summary>
        /// gets fired when the server recieves a kinect status package from the client
        /// </summary>
        /// <param name="input">input stream</param>
        /// <returns>a status message</returns>
        public String responseKinectStatus(Stream input)
        {
            // deserialize
            BinaryFormatter formatter = new BinaryFormatter();
            KinectStatusPackage status = (KinectStatusPackage)formatter.Deserialize(input);

            LogManager.writeLogDebug("[Webservice:ServerDefinition] Kinect Status Package recieved from ClientID " + status.clientID);
            OnKinectStatusPackageEvent(status);

            return "KINECT STATUS RECIEVED";
        }

        /// <summary>
        /// gets fired when the server recieves a scan request from a mobile client
        /// </summary>
        /// <param name="message">the input stream</param>
        /// <returns>status string wether succeeded or not</returns>
        public String responseAppRequest(Stream message)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AppRequestObject));
            AppRequestObject aro = (AppRequestObject)serializer.Deserialize(message);

            LogManager.writeLogDebug("[Webservice] App Request recieved from " + aro.clientAddress);

            String s = OnAppScanrequestEvent(aro);

            return s;
        }

        #endregion
    }
}
