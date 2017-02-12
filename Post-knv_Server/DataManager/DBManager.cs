using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Post_KNV_MessageClasses;
using Post_knv_Server.Log;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using Post_knv_Server.Webservice;
using System.Runtime.Serialization.Formatters.Binary;

namespace Post_knv_Server.DataManager
{
    /// <summary>
    /// this class represents the database manager, which has the task
    /// to handle all database related things
    /// </summary>
    public class DBManager
    {
        #region fields

        // the connection string for the database 
        String conString;

        #endregion

        #region external

        // event definitions for event updates
        public delegate void OnClientUpdatedEventHandler(ClientConfigObject pCco);
        public event OnClientUpdatedEventHandler OnClientUpdatedEvent;
        
        /// <summary>
        /// constructor
        /// </summary>
        public DBManager()
        {
            conString = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=" + Directory.GetCurrentDirectory() + @"\DBClientSpecification.mdf;Integrated Security=True";
        }

        /// <summary>
        /// the task the dbMamager has to do if a client sends a HelloRequest
        /// </summary>
        /// <param name="pHbo">the HBO</param>
        /// <returns>returns the CCO</returns>
        public ClientConfigObject recieveHelloRequest(HelloRequestObject pHbo)
        {
            try
            {
                //check if client already exists
                DataTable currentDataTable = getClientDataTable();
                DataColumn[] primaryKeyArray = new DataColumn[1];
                primaryKeyArray[0] = currentDataTable.Columns["ClientId"];
                currentDataTable.PrimaryKey = primaryKeyArray;

                //if exists, do
                if (currentDataTable.Rows.Contains(pHbo.ID))
                {
                    DataRow targetRow = currentDataTable.Rows.Find(pHbo.ID);

                    targetRow[2] = pHbo.ownIP;

                    //get object from DB
                    ClientConfigObject cco;
                    using (MemoryStream memStream = new MemoryStream((byte[])targetRow[3]))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        cco = (ClientConfigObject)formatter.Deserialize(memStream);
                        memStream.Close();
                    }

                    //change object
                    cco.ownIP = pHbo.ownIP;

                    //check if gateway is defined, if yes, send gateway info as target IP instead of server IP
                    if (Config.ServerConfigManager._ServerConfigObject.gatewayAddress == string.Empty)
                    {
                        cco.clientConnectionConfig.targetIP = Config.ServerConfigManager._ServerConfigObject.ownIP + ":" +
                            Config.ServerConfigManager._ServerConfigObject.listeningPort;
                    }
                    else
                    {
                        cco.clientConnectionConfig.targetIP = Config.ServerConfigManager._ServerConfigObject.gatewayAddress;
                    }
                    cco.clientRequestObject.isConnected = true;

                    //write object
                    using (MemoryStream wrStream = new MemoryStream())
                    {
                        BinaryFormatter formatterTwo = new BinaryFormatter();
                        formatterTwo.Serialize(wrStream, cco);
                        byte[] serInput = wrStream.ToArray();
                        targetRow[3] = (Object)serInput;
                        wrStream.Close();
                    }
                    
                    //update
                    updateKinectObjectInDB(currentDataTable);
                    return cco;
                }
                else
                {
                    //create new cco
                    ClientConfigObject cco = ClientConfigObject.createDefaultConfig();
                    cco.clientConnectionConfig.keepAliveInterval = Config.ServerConfigManager._ServerConfigObject.keepAliveInterval;
                    cco.ownIP = pHbo.ownIP;
                    cco.name = pHbo.Name;
                    cco.clientRequestObject.isConnected = true;

                    //check if gateway is defined, if yes, send gateway info as target IP instead of server IP
                    if (Config.ServerConfigManager._ServerConfigObject.gatewayAddress == string.Empty)
                    {
                        cco.clientConnectionConfig.targetIP = Config.ServerConfigManager._ServerConfigObject.ownIP + ":" +
                            Config.ServerConfigManager._ServerConfigObject.listeningPort;
                    }
                    else
                    {
                        cco.clientConnectionConfig.targetIP = Config.ServerConfigManager._ServerConfigObject.gatewayAddress;
                    }
                    cco.ID = getNewPrimaryID(currentDataTable);

                    //write object
                    BinaryFormatter formatter = new BinaryFormatter();
                    MemoryStream wrStream = new MemoryStream();
                    formatter.Serialize(wrStream, cco);
                    byte[] serInput = wrStream.ToArray();
                    wrStream.Close();
                    wrStream.Dispose();
                    
                    Object[] values = new Object[4];
                    values[0] = cco.ID;
                    values[1] = cco.name;
                    values[2] = cco.ownIP;
                    values[3] = (Object)serInput;
                    currentDataTable.Rows.Add(values);

                    //update the database
                    updateKinectObjectInDB(currentDataTable);
                    return cco;
                }
            }
            catch (Exception ex) { LogManager.writeLog("[DataManager:DBManager] ERROR: " + ex.Message); throw; }
        }

        /// <summary>
        /// returns a CCO based on the ID
        /// </summary>
        /// <param name="pID">the client ID</param>
        /// <returns>the CCO</returns>
        public ClientConfigObject getCcoByID(int pID)
        {
            DataTable currentDataTable = getClientDataTable();
            DataColumn[] primaryKeyArray = new DataColumn[1];
            primaryKeyArray[0] = currentDataTable.Columns["ClientId"];
            currentDataTable.PrimaryKey = primaryKeyArray;

            try
            {
                //get object from DB
                MemoryStream memStream = new MemoryStream((byte[])currentDataTable.Rows.Find(pID)[3]);
                BinaryFormatter formatter = new BinaryFormatter();
                ClientConfigObject cco = (ClientConfigObject)formatter.Deserialize(memStream);
                memStream.Close();
                return cco;                         
            }
            catch(Exception ex)
            {
                Log.LogManager.writeLog("[DataManager:DBManager] " + ex.Message);
            }

            return null;
        }

        /// <summary>
        /// delete the client from the list
        /// </summary>
        /// <param name="pID">the ID</param>
        public void deleteKinectFromDatabase(int pID)
        {
            DataTable currentDataTable = getClientDataTable();
            DataColumn[] primaryKeyArray = new DataColumn[1];
            primaryKeyArray[0] = currentDataTable.Columns["ClientId"];
            currentDataTable.PrimaryKey = primaryKeyArray;

            try
            {
                currentDataTable.Rows.Find(pID).Delete();
            }
            catch (Exception)
            {
                Log.LogManager.writeLog("[DataManager:DBManager] Delete of Client " + pID + " in database not possible.");
            }

            updateKinectObjectInDB(currentDataTable);
            Log.LogManager.writeLog("[DataManager:DBManager] Deletion of Client " + pID + " from database successful.");

        }

        /// <summary>
        /// get the client list from the database
        /// </summary>
        /// <returns>a list with all CCOs</returns>
        public List<ClientConfigObject> recieveClientList()
        {
            DataTable currentDataTable = getClientDataTable();
            DataColumn[] primaryKeyArray = new DataColumn[1];
            primaryKeyArray[0] = currentDataTable.Columns["ClientId"];
            currentDataTable.PrimaryKey = primaryKeyArray;

            List<ClientConfigObject> retList = new List<ClientConfigObject>();
            foreach(DataRow r in currentDataTable.Rows)
            {
                //get object from DB
                MemoryStream memStream = new MemoryStream((byte[])r[3]);
                BinaryFormatter formatter = new BinaryFormatter();
                ClientConfigObject cco = (ClientConfigObject)formatter.Deserialize(memStream);
                memStream.Close();

                retList.Add(cco);
            }
            return retList;
        }

        /// <summary>
        /// update the client infos
        /// </summary>
        /// <param name="pCco">the CCO</param>
        /// <param name="recievedFromClient">bool if request recieved from client or not</param>
        public void updateClient(ClientConfigObject pCco, bool recievedFromClient)
        {
            DataTable currentDataTable = getClientDataTable();
            DataColumn[] primaryKeyArray = new DataColumn[1];
            primaryKeyArray[0] = currentDataTable.Columns["ClientId"];
            currentDataTable.PrimaryKey = primaryKeyArray;

            try
            {
                DataRow row = currentDataTable.Rows.Find(pCco.ID);
                row[0] = pCco.ID;
                row[1] = pCco.name;
                row[2] = pCco.ownIP;

                //write object
                BinaryFormatter formatter = new BinaryFormatter();
                MemoryStream wrStream = new MemoryStream();
                formatter.Serialize(wrStream, pCco);
                byte[] serInput = wrStream.ToArray();

                row[3] = (Object)serInput;
            }
            catch (Exception ex)
            {
                Log.LogManager.writeLog("[DataManager:DBManager] Update of Client " + pCco.name + " in database not possible. Reason: " + ex.Message);
            }
            updateKinectObjectInDB(currentDataTable);
            if(!recievedFromClient) OnClientUpdatedEvent.BeginInvoke(pCco, null,null);
        }

        /// <summary>
        /// updates the database with the client kinect data
        /// </summary>
        /// <param name="input">the KDP</param>
        public void updateClient(KinectDataPackage input)
        {
            DataTable currentDataTable = getClientDataTable();
            DataColumn[] primaryKeyArray = new DataColumn[1];
            primaryKeyArray[0] = currentDataTable.Columns["ClientId"];
            currentDataTable.PrimaryKey = primaryKeyArray;
           
            try
            {
                DataRow row = currentDataTable.Rows.Find(input.usedConfig.ID);

                MemoryStream memStream = new MemoryStream();
                
                try
                {
                    BinaryFormatter bF = new BinaryFormatter();

                    bF.Serialize(memStream, input);

                    byte[] serInput = memStream.ToArray();

                    row[4] = serInput;
                }
                catch(Exception e)
                {
                    Log.LogManager.writeLog("[DataManager:DBManager] Update of Client " + input.usedConfig.name + " in database not possible. Reason: " + e.Message);
                }
                finally
                {
                    memStream.Close();
                }

                Log.LogManager.writeLog("[DataManager:DBManager] Update of Kinect Data of Client " + input.usedConfig.name + " in database successful.");
            }
            catch (Exception ex)
            {
                Log.LogManager.writeLog("[DataManager:DBManager] Update of Client with ID " + input.usedConfig.name + " in database not possible. Reason: " + ex.Message);
            }
            updateKinectObjectInDB(currentDataTable);

        }
        
        #endregion

        #region internal

        /// <summary>
        /// fetches the entire client list from the DB
        /// </summary>
        /// <returns>a data table containing all registered clients</returns>
        private DataTable getClientDataTable()
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(conString))
            {
                connection.Open();
                SqlDataAdapter adapter = new SqlDataAdapter();
                adapter.SelectCommand = new SqlCommand("select * from Clients", connection);
                adapter.Fill(dataTable);
            }
            return dataTable;
        }

        /// <summary>
        /// generates a new random and unique client id
        /// </summary>
        /// <param name="pInputTable">the current datatable containing all IDs</param>
        /// <returns>returns a free ID with ID between 0 and 1000</returns>
        private static int getNewPrimaryID(DataTable pInputTable)
        {
            List<int> currentIDlist = new List<int>();
            foreach(DataRow dr in pInputTable.Rows)
            {
                currentIDlist.Add((int)dr["ClientID"]);
            }

            Random rnd = new Random();
            int oID = rnd.Next(0, 1000);
            while(currentIDlist.Contains(oID))
            {
                oID = rnd.Next(0, 1000);
            }
            return oID;
        }

        /// <summary>
        /// perform an update of the kinectObject in the database
        /// </summary>
        /// <param name="pDataTable">the new data table</param>
        private void updateKinectObjectInDB(DataTable pDataTable)
        {
            using (SqlConnection connection = new SqlConnection(conString))
            {
                connection.Open();
                SqlDataAdapter adapter = new SqlDataAdapter();
                adapter.SelectCommand = new SqlCommand("select * from clients", connection);
                SqlCommandBuilder cb = new SqlCommandBuilder(adapter);
                adapter.UpdateCommand = cb.GetUpdateCommand();
                adapter.DeleteCommand = cb.GetDeleteCommand();
                
                adapter.Update(pDataTable);
            }

            LogManager.updateClientStatus();
        }

        #endregion

        /*
         * USE LATER OR DELETE
         * 
        /// <summary>
        /// retrieve the client kinect data package from the database
        /// </summary>
        /// <param name="id">the ID</param>
        /// <returns></returns>
        public KinectDataPackage getClientKinectData(int id)
        {
            DataTable currentDataTable = getClientDataTable();
            DataColumn[] primaryKeyArray = new DataColumn[1];
            primaryKeyArray[0] = currentDataTable.Columns["ClientId"];
            currentDataTable.PrimaryKey = primaryKeyArray;

            KinectDataPackage data = new KinectDataPackage();

            try
            {
                DataRow row = currentDataTable.Rows.Find(id);

                try
                {

                    MemoryStream memStream = new MemoryStream((byte []) row[4]);

                    BinaryFormatter binForm = new BinaryFormatter();
                    KinectDataPackage kdp = (KinectDataPackage)binForm.Deserialize(memStream);

                    Log.LogManager.writeLog("[DataManager:DBManager] Get KinectData of Client with name " + kdp.usedConfig.name + " from database.");
                    data = kdp;
                    return data;
                }
                catch (Exception ex)
                {
                    Log.LogManager.writeLog("[DataManager:DBManager] Get KinectData of Client with ID " + id + " from database not possible. Reason: " + ex.Message);
                }

            }
            catch (Exception ex)
            {
                Log.LogManager.writeLog("[DataManager:DBManager] Get KinectData of Client with ID " + id + " from database not possible. Reason: " + ex.Message);
            }

            return null;
        } */
    }
}
