/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using OpenMetaverse;
using log4net;
using OpenSim.Framework;

namespace OpenSim.Data.MSSQL
{
    /// <summary>
    /// A management class for the MS SQL Storage Engine
    /// </summary>
    public class MSSQLManager
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Connection string for ADO.net
        /// </summary>
        private readonly string connectionString;

        public MSSQLManager(string dataSource, string initialCatalog, string persistSecurityInfo, string userId,
                            string password)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

            builder.DataSource = dataSource;
            builder.InitialCatalog = initialCatalog;
            builder.PersistSecurityInfo = Convert.ToBoolean(persistSecurityInfo);
            builder.UserID = userId;
            builder.Password = password;
            builder.ApplicationName = Assembly.GetEntryAssembly().Location;

            connectionString = builder.ToString();
        }

        /// <summary>
        /// Initialize the manager and set the connectionstring
        /// </summary>
        /// <param name="connection"></param>
        public MSSQLManager(string connection)
        {
            connectionString = connection;
        }

        public SqlConnection DatabaseConnection()
        {
            SqlConnection conn = new SqlConnection(connectionString);

            //TODO is this good??? Opening connection here
            conn.Open();

            return conn;
        }

        //private DataTable createRegionsTable()
        //{
        //    DataTable regions = new DataTable("regions");

        //    createCol(regions, "regionHandle", typeof (ulong));
        //    createCol(regions, "regionName", typeof (String));
        //    createCol(regions, "uuid", typeof (String));

        //    createCol(regions, "regionRecvKey", typeof (String));
        //    createCol(regions, "regionSecret", typeof (String));
        //    createCol(regions, "regionSendKey", typeof (String));

        //    createCol(regions, "regionDataURI", typeof (String));
        //    createCol(regions, "serverIP", typeof (String));
        //    createCol(regions, "serverPort", typeof (String));
        //    createCol(regions, "serverURI", typeof (String));


        //    createCol(regions, "locX", typeof (uint));
        //    createCol(regions, "locY", typeof (uint));
        //    createCol(regions, "locZ", typeof (uint));

        //    createCol(regions, "eastOverrideHandle", typeof (ulong));
        //    createCol(regions, "westOverrideHandle", typeof (ulong));
        //    createCol(regions, "southOverrideHandle", typeof (ulong));
        //    createCol(regions, "northOverrideHandle", typeof (ulong));

        //    createCol(regions, "regionAssetURI", typeof (String));
        //    createCol(regions, "regionAssetRecvKey", typeof (String));
        //    createCol(regions, "regionAssetSendKey", typeof (String));

        //    createCol(regions, "regionUserURI", typeof (String));
        //    createCol(regions, "regionUserRecvKey", typeof (String));
        //    createCol(regions, "regionUserSendKey", typeof (String));

        //    createCol(regions, "regionMapTexture", typeof (String));
        //    createCol(regions, "serverHttpPort", typeof (String));
        //    createCol(regions, "serverRemotingPort", typeof (uint));

        //    // Add in contraints
        //    regions.PrimaryKey = new DataColumn[] {regions.Columns["UUID"]};
        //    return regions;
        //}

        /// <summary>
        ///
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        protected static void createCol(DataTable dt, string name, Type type)
        {
            DataColumn col = new DataColumn(name, type);
            dt.Columns.Add(col);
        }

        /// <summary>
        /// Define Table function
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        protected static string defineTable(DataTable dt)
        {
            string sql = "create table " + dt.TableName + "(";
            string subsql = String.Empty;
            foreach (DataColumn col in dt.Columns)
            {
                if (subsql.Length > 0)
                {
                    // a map function would rock so much here
                    subsql += ",\n";
                }

                subsql += col.ColumnName + " " + SqlType(col.DataType);
                if (col == dt.PrimaryKey[0])
                {
                    subsql += " primary key";
                }
            }
            sql += subsql;
            sql += ")";
            return sql;
        }

        /// <summary>
        /// Type conversion function
        /// </summary>
        /// <param name="type">a type</param>
        /// <returns>a sqltype</returns>
        /// <remarks>this is something we'll need to implement for each db slightly differently.</remarks>
        public static string SqlType(Type type)
        {
            if (type == typeof(String))
            {
                return "varchar(255)";
            }
            else if (type == typeof(Int32))
            {
                return "integer";
            }
            else if (type == typeof(Double))
            {
                return "float";
            }
            else if (type == typeof(Byte[]))
            {
                return "image";
            }
            else
            {
                return "varchar(255)";
            }
        }

        /// <summary>
        /// Type conversion to a SQLDbType functions
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal SqlDbType DbtypeFromType(Type type)
        {
            if (type == typeof(string))
            {
                return SqlDbType.VarChar;
            }
            if (type == typeof(double))
            {
                return SqlDbType.Float;
            }
            if (type == typeof(int))
            {
                return SqlDbType.Int;
            }
            if (type == typeof(bool))
            {
                return SqlDbType.Bit;
            }
            if (type == typeof(UUID))
            {
                return SqlDbType.VarChar;
            }
            if (type == typeof(Byte[]))
            {
                return SqlDbType.Image;
            }
            if (type == typeof(uint))
            {
                return SqlDbType.Int;
            }
            return SqlDbType.VarChar;
        }

        /// <summary>
        /// Creates value for parameter.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private static object CreateParameterValue(object value)
        {
            Type valueType = value.GetType();

            if (valueType == typeof(UUID))
            {
                return value.ToString();
            }
            if (valueType == typeof(bool))
            {
                return (bool)value ? 1 : 0;
            }
            if (valueType == typeof(Byte[]))
            {
                return value;
            }
            return value;
        }

        /// <summary>
        /// Create a parameter for a command
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="parameterObject">parameter object.</param>
        /// <returns></returns>
        internal SqlParameter CreateParameter(string parameterName, object parameterObject)
        {
            return CreateParameter(parameterName, parameterObject, false);
        }

        /// <summary>
        /// Creates the parameter for a command.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="parameterObject">parameter object.</param>
        /// <param name="parameterOut">if set to <c>true</c> parameter is a output parameter</param>
        /// <returns></returns>
        internal SqlParameter CreateParameter(string parameterName, object parameterObject, bool parameterOut)
        {
            //Tweak so we dont always have to add @ sign
            if (!parameterName.StartsWith("@")) parameterName = "@" + parameterName;

            SqlParameter parameter = new SqlParameter(parameterName, DbtypeFromType(parameterObject.GetType()));

            if (parameterOut)
            {
                parameter.Direction = ParameterDirection.Output;
            }
            else
            {
                parameter.Direction = ParameterDirection.Input;
                parameter.Value = CreateParameterValue(parameterObject);
            }

            return parameter;
        }

        private static readonly Dictionary<string, string> emptyDictionary = new Dictionary<string, string>();
        internal AutoClosingSqlCommand Query(string sql)
        {
            return Query(sql, emptyDictionary);
        }

        /// <summary>
        /// Runs a query with protection against SQL Injection by using parameterised input.
        /// </summary>
        /// <param name="sql">The SQL string - replace any variables such as WHERE x = "y" with WHERE x = @y</param>
        /// <param name="parameters">The parameters - index so that @y is indexed as 'y'</param>
        /// <returns>A Sql DB Command</returns>
        internal AutoClosingSqlCommand Query(string sql, Dictionary<string, string> parameters)
        {
            SqlCommand dbcommand = DatabaseConnection().CreateCommand();
            dbcommand.CommandText = sql;
            foreach (KeyValuePair<string, string> param in parameters)
            {
                dbcommand.Parameters.AddWithValue(param.Key, param.Value);
            }

            return new AutoClosingSqlCommand(dbcommand);
        }

        /// <summary>
        /// Runs a database reader object and returns a region row
        /// </summary>
        /// <param name="reader">An active database reader</param>
        /// <returns>A region row</returns>
        public RegionProfileData getRegionRow(IDataReader reader)
        {
            RegionProfileData regionprofile = new RegionProfileData();

            if (reader.Read())
            {
                // Region Main
                regionprofile.regionHandle = Convert.ToUInt64(reader["regionHandle"]);
                regionprofile.regionName = (string)reader["regionName"];
                regionprofile.UUID = new UUID((string)reader["uuid"]);

                // Secrets
                regionprofile.regionRecvKey = (string)reader["regionRecvKey"];
                regionprofile.regionSecret = (string)reader["regionSecret"];
                regionprofile.regionSendKey = (string)reader["regionSendKey"];

                // Region Server
                regionprofile.regionDataURI = (string)reader["regionDataURI"];
                regionprofile.regionOnline = false; // Needs to be pinged before this can be set.
                regionprofile.serverIP = (string)reader["serverIP"];
                regionprofile.serverPort = Convert.ToUInt32(reader["serverPort"]);
                regionprofile.serverURI = (string)reader["serverURI"];
                regionprofile.httpPort = Convert.ToUInt32(reader["serverHttpPort"]);
                regionprofile.remotingPort = Convert.ToUInt32(reader["serverRemotingPort"]);


                // Location
                regionprofile.regionLocX = Convert.ToUInt32(reader["locX"]);
                regionprofile.regionLocY = Convert.ToUInt32(reader["locY"]);
                regionprofile.regionLocZ = Convert.ToUInt32(reader["locZ"]);

                // Neighbours - 0 = No Override
                regionprofile.regionEastOverrideHandle = Convert.ToUInt64(reader["eastOverrideHandle"]);
                regionprofile.regionWestOverrideHandle = Convert.ToUInt64(reader["westOverrideHandle"]);
                regionprofile.regionSouthOverrideHandle = Convert.ToUInt64(reader["southOverrideHandle"]);
                regionprofile.regionNorthOverrideHandle = Convert.ToUInt64(reader["northOverrideHandle"]);

                // Assets
                regionprofile.regionAssetURI = (string)reader["regionAssetURI"];
                regionprofile.regionAssetRecvKey = (string)reader["regionAssetRecvKey"];
                regionprofile.regionAssetSendKey = (string)reader["regionAssetSendKey"];

                // Userserver
                regionprofile.regionUserURI = (string)reader["regionUserURI"];
                regionprofile.regionUserRecvKey = (string)reader["regionUserRecvKey"];
                regionprofile.regionUserSendKey = (string)reader["regionUserSendKey"];
                regionprofile.owner_uuid = new UUID((string) reader["owner_uuid"]);
                // World Map Addition
                string tempRegionMap = reader["regionMapTexture"].ToString();
                if (tempRegionMap != String.Empty)
                {
                    regionprofile.regionMapTextureID = new UUID(tempRegionMap);
                }
                else
                {
                    regionprofile.regionMapTextureID = new UUID();
                }
            }
            else
            {
                reader.Close();
                throw new Exception("No rows to return");
            }
            return regionprofile;
        }

        /// <summary>
        /// Reads a user profile from an active data reader
        /// </summary>
        /// <param name="reader">An active database reader</param>
        /// <returns>A user profile</returns>
        public UserProfileData readUserRow(IDataReader reader)
        {
            UserProfileData retval = new UserProfileData();

            if (reader.Read())
            {
                retval.ID = new UUID((string)reader["UUID"]);
                retval.FirstName = (string)reader["username"];
                retval.SurName = (string)reader["lastname"];

                retval.PasswordHash = (string)reader["passwordHash"];
                retval.PasswordSalt = (string)reader["passwordSalt"];

                retval.HomeRegion = Convert.ToUInt64(reader["homeRegion"].ToString());
                retval.HomeLocation = new Vector3(
                    Convert.ToSingle(reader["homeLocationX"].ToString()),
                    Convert.ToSingle(reader["homeLocationY"].ToString()),
                    Convert.ToSingle(reader["homeLocationZ"].ToString()));
                retval.HomeLookAt = new Vector3(
                    Convert.ToSingle(reader["homeLookAtX"].ToString()),
                    Convert.ToSingle(reader["homeLookAtY"].ToString()),
                    Convert.ToSingle(reader["homeLookAtZ"].ToString()));

                retval.Created = Convert.ToInt32(reader["created"].ToString());
                retval.LastLogin = Convert.ToInt32(reader["lastLogin"].ToString());

                retval.UserInventoryURI = (string)reader["userInventoryURI"];
                retval.UserAssetURI = (string)reader["userAssetURI"];

                retval.CanDoMask = Convert.ToUInt32(reader["profileCanDoMask"].ToString());
                retval.WantDoMask = Convert.ToUInt32(reader["profileWantDoMask"].ToString());

                retval.AboutText = (string)reader["profileAboutText"];
                retval.FirstLifeAboutText = (string)reader["profileFirstText"];

                retval.Image = new UUID((string)reader["profileImage"]);
                retval.FirstLifeImage = new UUID((string)reader["profileFirstImage"]);
                retval.WebLoginKey = new UUID((string)reader["webLoginKey"]);
            }
            else
            {
                return null;
            }
            return retval;
        }

        /// <summary>
        /// Reads an agent row from a database reader
        /// </summary>
        /// <param name="reader">An active database reader</param>
        /// <returns>A user session agent</returns>
        public UserAgentData readAgentRow(IDataReader reader)
        {
            UserAgentData retval = new UserAgentData();

            if (reader.Read())
            {
                // Agent IDs
                retval.ProfileID = new UUID((string)reader["UUID"]);
                retval.SessionID = new UUID((string)reader["sessionID"]);
                retval.SecureSessionID = new UUID((string)reader["secureSessionID"]);

                // Agent Who?
                retval.AgentIP = (string)reader["agentIP"];
                retval.AgentPort = Convert.ToUInt32(reader["agentPort"].ToString());
                retval.AgentOnline = Convert.ToInt32(reader["agentOnline"].ToString()) != 0;

                // Login/Logout times (UNIX Epoch)
                retval.LoginTime = Convert.ToInt32(reader["loginTime"].ToString());
                retval.LogoutTime = Convert.ToInt32(reader["logoutTime"].ToString());

                // Current position
                retval.Region = (string)reader["currentRegion"];
                retval.Handle = Convert.ToUInt64(reader["currentHandle"].ToString());
                Vector3 tmp_v;
                Vector3.TryParse((string)reader["currentPos"], out tmp_v);
                retval.Position = tmp_v;

            }
            else
            {
                return null;
            }
            return retval;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public AssetBase getAssetRow(IDataReader reader)
        {
            AssetBase asset = new AssetBase();
            if (reader.Read())
            {
                // Region Main
                asset = new AssetBase();
                asset.Data = (byte[])reader["data"];
                asset.Description = (string)reader["description"];
                asset.FullID = new UUID((string)reader["id"]);
                asset.Local = Convert.ToBoolean(reader["local"]); // ((sbyte)reader["local"]) != 0 ? true : false;
                asset.Name = (string)reader["name"];
                asset.Type = Convert.ToSByte(reader["assetType"]);
            }
            else
            {
                return null; // throw new Exception("No rows to return");
            }
            return asset;
        }


        /// <summary>
        /// Inserts a new row into the log database
        /// </summary>
        /// <param name="serverDaemon">The daemon which triggered this event</param>
        /// <param name="target">Who were we operating on when this occured (region UUID, user UUID, etc)</param>
        /// <param name="methodCall">The method call where the problem occured</param>
        /// <param name="arguments">The arguments passed to the method</param>
        /// <param name="priority">How critical is this?</param>
        /// <param name="logMessage">Extra message info</param>
        /// <returns>Saved successfully?</returns>
        public bool insertLogRow(string serverDaemon, string target, string methodCall, string arguments, int priority,
                                 string logMessage)
        {
            string sql = "INSERT INTO logs ([target], [server], [method], [arguments], [priority], [message]) VALUES ";
            sql += "(@target, @server, @method, @arguments, @priority, @message);";

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["server"] = serverDaemon;
            parameters["target"] = target;
            parameters["method"] = methodCall;
            parameters["arguments"] = arguments;
            parameters["priority"] = priority.ToString();
            parameters["message"] = logMessage;

            bool returnval = false;

            using (IDbCommand result = Query(sql, parameters))
            {
                try
                {

                    if (result.ExecuteNonQuery() == 1)
                        returnval = true;

                }
                catch (Exception e)
                {
                    m_log.Error(e.ToString());
                    return false;
                }
            }

            return returnval;
        }

        /// <summary>
        /// Execute a SQL statement stored in a resource, as a string
        /// </summary>
        /// <param name="name">the ressource string</param>
        public void ExecuteResourceSql(string name)
        {
            using (IDbCommand cmd = Query(getResourceString(name), new Dictionary<string,string>()))
            {
                cmd.ExecuteNonQuery();
            }
        }


        /// <summary>
        /// Given a list of tables, return the version of the tables, as seen in the database
        /// </summary>
        /// <param name="tableList"></param>
        public void GetTableVersion(Dictionary<string, string> tableList)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            param["dbname"] = new SqlConnectionStringBuilder(connectionString).InitialCatalog;

            using (IDbCommand tablesCmd =
                Query("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_CATALOG=@dbname", param))
            using (IDataReader tables = tablesCmd.ExecuteReader())
            {
                while (tables.Read())
                {
                    try
                    {
                        string tableName = (string)tables["TABLE_NAME"];
                        if (tableList.ContainsKey(tableName))
                            tableList[tableName] = tableName;
                    }
                    catch (Exception e)
                    {
                        m_log.Error(e.ToString());
                    }
                }
                tables.Close();
            }

        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string getResourceString(string name)
        {
            Assembly assem = GetType().Assembly;
            string[] names = assem.GetManifestResourceNames();

            foreach (string s in names)
                if (s.EndsWith(name))
                    using (Stream resource = assem.GetManifestResourceStream(s))
                    {
                        using (StreamReader resourceReader = new StreamReader(resource))
                        {
                            string resourceString = resourceReader.ReadToEnd();
                            return resourceString;
                        }
                    }
            throw new Exception(string.Format("Resource '{0}' was not found", name));
        }

        /// <summary>
        /// Returns the version of this DB provider
        /// </summary>
        /// <returns>A string containing the DB provider</returns>
        public string getVersion()
        {
            Module module = GetType().Module;
            // string dllName = module.Assembly.ManifestModule.Name;
            Version dllVersion = module.Assembly.GetName().Version;

            return
                string.Format("{0}.{1}.{2}.{3}", dllVersion.Major, dllVersion.Minor, dllVersion.Build,
                              dllVersion.Revision);
        }

        public bool insertAgentRow(UserAgentData agentdata)
        {
            string sql = @"

IF EXISTS (SELECT * FROM agents WHERE UUID = @UUID)
    BEGIN
        UPDATE agents SET UUID = @UUID, sessionID = @sessionID, secureSessionID = @secureSessionID, agentIP = @agentIP, agentPort = @agentPort, agentOnline = @agentOnline, loginTime = @loginTime, logoutTime = @logoutTime, currentRegion = @currentRegion, currentHandle = @currentHandle, currentPos = @currentPos
        WHERE UUID = @UUID
    END
ELSE
    BEGIN
        INSERT INTO 
            agents (UUID, sessionID, secureSessionID, agentIP, agentPort, agentOnline, loginTime, logoutTime, currentRegion, currentHandle, currentPos) VALUES 
            (@UUID, @sessionID, @secureSessionID, @agentIP, @agentPort, @agentOnline, @loginTime, @logoutTime, @currentRegion, @currentHandle, @currentPos)
    END
";

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            parameters["@UUID"] = agentdata.ProfileID.ToString();
            parameters["@sessionID"] = agentdata.SessionID.ToString();
            parameters["@secureSessionID"] = agentdata.SecureSessionID.ToString();
            parameters["@agentIP"] = agentdata.AgentIP.ToString();
            parameters["@agentPort"] = agentdata.AgentPort.ToString();
            parameters["@agentOnline"] = (agentdata.AgentOnline == true) ? "1" : "0";
            parameters["@loginTime"] = agentdata.LoginTime.ToString();
            parameters["@logoutTime"] = agentdata.LogoutTime.ToString();
            parameters["@currentRegion"] = agentdata.Region.ToString();
            parameters["@currentHandle"] = agentdata.Handle.ToString();
            parameters["@currentPos"] = "<" + ((int)agentdata.Position.X).ToString() + "," + ((int)agentdata.Position.Y).ToString() + "," + ((int)agentdata.Position.Z).ToString() + ">";


            using (IDbCommand result = Query(sql, parameters))
            {
                result.Transaction = result.Connection.BeginTransaction(IsolationLevel.Serializable);
                try
                {
                    if (result.ExecuteNonQuery() > 0)
                    {
                        result.Transaction.Commit();
                        return true;
                    }
                    else
                    {
                        result.Transaction.Rollback();
                        return false;
                    }
                }
                catch (Exception e)
                {
                    result.Transaction.Rollback();
                    m_log.Error(e.ToString());
                    return false;
                }
            }

        }
    }
}
