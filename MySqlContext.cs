using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using System.Linq;

namespace DataBaseService
{
    public class MySqlContext : IDataBaseService
    {
        string connectionString;
        StateChangeEventHandler ConnectionStatusDelegate;
        static Logger logger = LogManager.GetCurrentClassLogger();

        MySqlConnection GetConnection()
        {
            var dbConn = new MySqlConnection(connectionString);
            dbConn.StateChange -= ConnectionStatusDelegate;
            dbConn.StateChange += ConnectionStatusDelegate;
            return dbConn;
        }

        /// <summary>
        /// Builds a connection string to be used for getting MySql connection to the server, based on a connection string
        /// </summary>
        /// <param name="connectionString"></param>
        public MySqlContext(string connectionString) => this.connectionString = connectionString;

        /// <summary>
        /// Builds a connection string to be used for getting MySql connection to the server, based on the IConnectionParameters.
        /// </summary>
        /// <param name="cp">Parameter set as IConnectionParameters</param>
        public MySqlContext(IConnectionParameters cp) : this(cp.ServerAddress, cp.ServerPort, cp.UserName, cp.Password, cp.DBName, cp.CharSet, cp.SSLMode) { }
        public MySqlContext(IConnectionParameters cp, StateChangeEventHandler connectionStatus) : this(cp.ServerAddress, cp.ServerPort, cp.UserName, cp.Password, cp.DBName, cp.CharSet, cp.SSLMode, connectionStatus) { }

        /// <summary>
        /// Builds a connection string to be used for getting MySql connection to the server, based on parameters.
        /// </summary>
        /// <param name="serverAddress">Name of the server to connect to</param>
        /// <param name="serverPort">Port number to be used for connecting with the server</param>
        /// <param name="userName">User name to be used to connect with a database</param>
        /// <param name="passwd">User password to be used to make a connection</param>
        /// <param name="dbName">Name of the database to connect to</param>
        /// <param name="charset">Character set to be used for sending queries to the server</param>
        /// <param name="sslMode">Indicates whether to use SSL mode for the connection (0 - no SSL, 1 - preferred, 2 - required, 3 - verifyCA, 4 - verifyFull)</param>
        public MySqlContext(string serverAddress, uint serverPort, string userName, string passwd, string dbName, string charset, int sslMode)
        {
            var connString = new MySqlConnectionStringBuilder()
            {
                Server = serverAddress,
                UserID = userName,
                Password = passwd,
                Database = dbName,
                CharacterSet = charset,
                SslMode = (MySqlSslMode)sslMode,
                Pooling = true,
                MinimumPoolSize = 1,
                MaximumPoolSize = 10,
                Port = serverPort
            };

            connectionString = connString.ToString();
        }
        public MySqlContext(string serverAddress, uint serverPort, string userName, string passwd, string dbName, string charset, int sslMode, StateChangeEventHandler connectionStatus)
        {
            ConnectionStatusDelegate = connectionStatus;
            var connString = new MySqlConnectionStringBuilder()
            {
                Server = serverAddress,
                UserID = userName,
                Password = passwd,
                Database = dbName,
                CharacterSet = charset,
                SslMode = (MySqlSslMode)sslMode,
                Pooling = true,
                MinimumPoolSize = 1,
                MaximumPoolSize = 10,
                Port = serverPort
            };

            connectionString = connString.ToString();
        }
        /// <summary>
        /// The async method is getting records from database and returning them as a task of IEnumerable set of items.
        /// </summary>
        /// <typeparam name="T">The name of the type to work on</typeparam>
        /// <param name="sqlString">Sql SELECT string to get data from database.</param>
        /// <param name="GetDataRow">Generic callBack method to create an object. It requires IDataReader parameter to build an object of pointed type.</param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> FetchRecordSetAsync<T>(string sqlString, GetData<T> GetDataRow)
        {
            var HS = new HashSet<T>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var t = conn.BeginTransaction();
                    try
                    {
                        using (var R = await new MySqlCommand { CommandText = sqlString, Connection = conn, Transaction = t }.ExecuteReaderAsync())
                        {
                            while (R.Read()) HS.Add(GetDataRow(R));
                        }
                        t.Commit();
                    }
                    catch (MySqlException ex)
                    {
                        logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");
                        t.Rollback();
                    }
                }
                return await Task.FromResult(HS);
            }
            catch (Exception ex)
            {
                logger.Error($"\nŹródło błedu: {ex.Source};  Kod błędu: {ex.Message}\n{ex.ToString()}\n");
                return await Task.FromResult(HS);
            }
        }

        public async Task<IEnumerable<T>> FetchRecordSetAsync<T>(string sqlString, object[] sqlParameterValue, DataParameters createParams, GetData<T> GetDataRow)
        {
            var HS = new HashSet<T>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var t = conn.BeginTransaction();
                    try
                    {
                        using (var cmd = new MySqlCommand { CommandText = sqlString, Connection = conn, Transaction = t })
                        {
                            createParams(cmd);
                            for (int i = 0; i < sqlParameterValue.Length; i++)
                                cmd.Parameters[i].Value = sqlParameterValue[i];

                            var R = await cmd.ExecuteReaderAsync();
                            while (R.Read()) HS.Add(GetDataRow(R));
                        }
                        t.Commit();
                    }
                    catch (MySqlException ex)
                    {
                        t.Rollback();
                        logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");

                        throw new Exception(ex.Message);
                    }
                }
                return await Task.FromResult(HS);
            }
            catch (Exception ex)
            {
                logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Message}\n{ex.ToString()}");
                return await Task.FromResult(HS);
            }
        }

        public async Task<T> FetchRecordAsync<T>(string sqlString, GetData<T> GetDataRow)
        {
            T DR = default;
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var t = conn.BeginTransaction();
                    try
                    {
                        using (var R = await new MySqlCommand { CommandText = sqlString, Connection = conn, Transaction = t }.ExecuteReaderAsync())
                        {
                            if (R.Read()) DR = GetDataRow(R);
                        }
                        t.Commit();
                    }
                    catch (MySqlException ex)
                    {
                        t.Rollback();
                        logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");
                        throw new Exception(ex.Message);
                    }
                }
                return await Task.FromResult(DR);
            }
            catch (Exception ex)
            {
                logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Message}\n{ex.ToString()}");
                return await Task.FromResult(DR);
            }
        }
        public async Task<string> FetchSingleValueAsync(string sqlString)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var T = conn.BeginTransaction();
                var cmd = new MySqlCommand { CommandText = sqlString, Connection = conn, Transaction = T };
                try
                {
                    var value = await cmd.ExecuteScalarAsync();
                    T.Commit();
                    return await Task.FromResult(value.ToString());
                }
                catch (MySqlException ex)
                {
                    T.Rollback();
                    logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");

                    throw new Exception(ex.Message);
                    //return String.Empty;
                }
            }
        }
        public async Task<int> AddManyRecordsAsync(string sqlString, ISet<object[]> sqlParamValue, DataParameters createParams)
        {
            return await ExecuteManyCommandsAsync(sqlString, sqlParamValue, createParams);
        }
        public async Task<int> AddManyRecordsAsync(string sqlString, ISet<IDictionary<string, object>> sqlParamWithValue)
        {
            return await ExecuteManyCommandsAsync(sqlString, sqlParamWithValue);
        }

        /// <summary>
        /// Adds one record to database
        /// </summary>
        /// <param name="sqlString"> String sql to add data</param>
        /// <param name="sqlParamValue">Parameters values that are to be added</param>
        /// <param name="createParams"> A callback method to create parameters due to operation</param>
        /// <returns>Record ID that was just added</returns>
        public async Task<int> AddRecordAsync(string sqlString, object[] sqlParamValue, DataParameters createParams)
        {
            await ExecuteCommandAsync(sqlString, sqlParamValue, createParams);
            int.TryParse(await FetchSingleValueAsync("SELECT LAST_INSERT_ID();"), out int lastInsertedID);
            return await Task.FromResult(lastInsertedID);
        }

        public async Task<int> UpdateRecordAsync(string sqlString, object[] sqlParameterValue, DataParameters updateParams)
        {
            return await ExecuteCommandAsync(sqlString, sqlParameterValue, updateParams);
        }

        public async Task<int> RemoveRecordAsync(string sqlString, object[] sqlParameterValue, DataParameters delParams)
        {
            return await ExecuteCommandAsync(sqlString, sqlParameterValue, delParams);
        }

        public async Task<int> RemoveManyRecordsAsync(string sqlString, ISet<object[]> sqlParameterValue, DataParameters delParams)
        {
            return await ExecuteManyCommandsAsync(sqlString, sqlParameterValue, delParams);
        }
        public async Task<int> ExecuteManyCommandsAsync(string sqlString, ISet<object[]> sqlParamValue, DataParameters createParams)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var T = conn.BeginTransaction();
                //var cmd = CreateCommand(conn, T, CommandType.Text, sqlString);
                var cmd = new MySqlCommand { CommandText = sqlString, Connection = conn, Transaction = T };
                createParams(cmd);
                int recordAffected = 0;
                try
                {
                    foreach (var p in sqlParamValue) recordAffected += await ExecuteCommandAsync(cmd, p);
                    T.Commit();
                }
                catch (MySqlException ex)
                {
                    T.Rollback();
                    //recordAffected = 0;
                    logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");
                    throw new Exception(ex.Message);
                }
                return recordAffected;
            }
        }
        public async Task<int> ExecuteManyCommandsAsync(string sqlString, ISet<IDictionary<string, object>> sqlParamWithValue)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var T = conn.BeginTransaction();
                using (var cmd = new MySqlCommand { CommandText = sqlString, Connection = conn, Transaction = T })
                {
                    cmd.Prepare();
                    foreach ( var p in sqlParamWithValue.First()) cmd.Parameters.AddWithValue(p.Key, p.Value);
                    int recordAffected = 0;
                    try
                    {
                        foreach (var p in sqlParamWithValue) recordAffected += await ExecuteCommandAsync(cmd, p);
                        T.Commit();
                    }
                    catch (MySqlException ex)
                    {
                        T.Rollback();
                        logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");
                        throw new Exception(ex.Message);
                    }
                    return recordAffected;
                }
            }
        }

        //private void CreateParams(MySqlCommand cmd, IDictionary<string, object> paramsWithValues)
        //{
        //    foreach (var p in paramsWithValues)
        //    {
        //        cmd.Parameters.AddWithValue(p.Key, p.Value);
        //    }
        //}

        async Task<int> ExecuteCommandAsync(string sqlString, object[] sqlParamValue, DataParameters createParams)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var T = conn.BeginTransaction();
                //var cmd = CreateCommand(conn, T, CommandType.Text, sqlString);
                var cmd = new MySqlCommand { CommandText = sqlString, Connection = conn, Transaction = T };

                createParams(cmd);
                try
                {
                    var recordAffected = await ExecuteCommandAsync(cmd, sqlParamValue);
                    T.Commit();
                    return recordAffected;
                }
                catch (MySqlException ex)
                {
                    logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");
                    T.Rollback();
                    return 0;
                }
            }
        }

        async Task<int> ExecuteCommandAsync(MySqlCommand cmd, object[] sqlParamValue)
        {
            using (cmd)
            {
                int count = 0;
                try
                {
                    for (int i = 0; i < sqlParamValue.Length; i++)
                        cmd.Parameters[i].Value = sqlParamValue[i];
                    count = await cmd.ExecuteNonQueryAsync();
                }
                catch (MySqlException ex)
                {
                    logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");

                    switch (ex.Number)
                    {
                        case 1062:
                            return 0;
                        default:
                            throw;
                    }
                }
                return count;
            }
        }
        public async Task<int> ExecuteCommandAsync(MySqlCommand cmd, IDictionary<string, object> paramsWithValues)
        {
            try
            {
                //for (int i = 0; i < sqlParamValue.Length; i++)
                //    cmd.Parameters[i].Value = sqlParamValue[i];
                foreach (var p in paramsWithValues) cmd.Parameters[p.Key].Value = p.Value;
                return await cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");

                switch (ex.Number)
                {
                    case 1062:
                        return 0;
                    default:
                        throw;
                }
            }
        }

        public bool TestConnection()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    return conn.Ping();
                }
            }
            catch (MySqlException ex)
            {
                logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");
                throw new Exception(ex.Message);
            }
        }
    }
}
