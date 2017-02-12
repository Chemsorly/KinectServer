using Post_knv_Server.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Post_knv_Server.Config
{
    /// <summary>
    /// this class represents the server config manager
    /// </summary>
    public class ServerConfigManager
    {
        public static ServerConfigObject _ServerConfigObject;
        
        static String configFilePath = @"config\";
        static String configFileName = @"ServerConfig.dat";

        #region import/export functions

        /// <summary>
        /// the server config manager, which has the task
        /// to manage the server configuration files for the
        /// clients
        /// </summary>
        public ServerConfigManager()
        {
            try
            {
                if (checkForConfigFile(configFilePath + configFileName))
                {
                    _ServerConfigObject = loadConfigFromFile(configFilePath + configFileName);
                    LogManager.writeLog("[ConfigManager:ConfigManager] Config loaded from file");
                }
                else
                {
                    _ServerConfigObject = ServerConfigObject.GetDefaultConfig();
                    LogManager.writeLog("[ConfigManager:ConfigManager] Default config loaded");
                }
            }
            catch (Exception e)
            {
                LogManager.writeLog("[ConfigManager:ConfigManager] " + e.ToString());
                _ServerConfigObject = ServerConfigObject.GetDefaultConfig();
            }

            saveConfig();
            LogManager.writeLog("[ConfigManager:ConfigManager] Successfully initialized");
        }

        /// <summary>
        /// writes the config
        /// </summary>
        public void writeConfig()
        {
            saveConfig();
        }

        /// <summary>
        /// saves the config
        /// </summary>
        private static void saveConfig()
        {
            saveConfigToFile(_ServerConfigObject, configFilePath, configFileName);
        }

        /// <summary>
        /// checks if there is a config file already present
        /// </summary>
        /// <param name="pConfigFilePath">the file path</param>
        /// <returns>true if exists, false if not</returns>
        static bool checkForConfigFile(String pConfigFilePath)
        {
            if (File.Exists(pConfigFilePath))
                return true;
            return false;
        }

        /// <summary>
        /// this represents a server config object loaded from file
        /// </summary>
        /// <param name="inputPath">the input file path</param>
        /// <returns>a server config object</returns>
        static ServerConfigObject loadConfigFromFile(String inputPath)
        {
            FileStream stream = new FileStream(inputPath, FileMode.Open);
            BinaryFormatter formatter = new BinaryFormatter();
            ServerConfigObject _returnObject = (ServerConfigObject)formatter.Deserialize(stream);
            stream.Close();

            _returnObject.ownIP = Post_KNV_MessageClasses.ClientConfigObject.getOwnIP();
            return _returnObject;
        }

        /// <summary>
        /// save the given config to file
        /// </summary>
        /// <param name="inputObject">the server config object</param>
        /// <param name="configFilePath">the file path</param>
        /// <param name="configFileName">the file name</param>
        static void saveConfigToFile(ServerConfigObject inputObject, String configFilePath, String configFileName)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = null;
            if (!checkForConfigFile(configFilePath + configFileName)) Directory.CreateDirectory(configFilePath);
            try
            {
                stream = new FileStream(configFilePath + configFileName, FileMode.Create);                
                formatter.Serialize(stream, inputObject);
                stream.Flush();
                stream.Close();                
            }
            catch (Exception ex) { Log.LogManager.writeLog("ERROR: " + ex.Message); }
            finally { if(stream != null) {stream.Close(); stream.Dispose();} }
        }

        #endregion
    }
}
