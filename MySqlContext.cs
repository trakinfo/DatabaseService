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
        /// <param name="connectionString">Ready to use string to connect to database</param>
        public MySqlContext(string connectionString) => this.connectionString = connectionString;
        /// <summary>
        /// Builds a connection string to be used for getting MySql connection to the server, based on a connection string and StateChangeEventHandler delegate
        /// </summary>
        /// <param name="connectionString">Ready to use string to connect to database</param>
        /// <param name="connectionStatus">Callback method injected to track state of connection</param>
        public MySqlContext(string connectionString, StateChangeEventHandler connectionStatus) : this(connectionString) { ConnectionStatusDelegate = connectionStatus; }

        /// <summary>
        /// Builds a connection string to be used for getting MySql connection to the server, based on the IConnectionParameters.
        /// </summary>
        /// <param name="cp">Parameter set of IConnectionParameters type</param>
        public MySqlContext(IConnectionParameters cp) : this(cp.ServerAddress, cp.ServerPort, cp.UserName, cp.Password, cp.DBName, cp.CharSet, cp.SSLMode) { }
        /// <summary>
        /// Builds a connection string to be used for getting MySql connection to the server, based on the IConnectionParameters and StateChangeEventHandler delegate.
        /// </summary>
        /// <param name="cp">Parameter set of IConnectionParameters type</param>
        /// <param name="connectionStatus">Callback method injected to track state of connection</param>
        public MySqlContext(IConnectionParameters cp, StateChangeEventHandler connectionStatus) : this(cp) { ConnectionStatusDelegate = connectionStatus; }

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

        public async Task<IEnumerable<T>> FetchRecordSetAsync<T>(string sqlString, IDictionary<string, object> sqlParameterWithValue, GetData<T> GetDataRow)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var t = conn.BeginTransaction();
                    try
                    {
                        var HS = new HashSet<T>();
                        IDataReader R;
                        using (var cmd = new MySqlCommand { CommandText = sqlString, Connection = conn, Transaction = t })
                        {
                            foreach (var p in sqlParameterWithValue) cmd.Parameters.AddWithValue(p.Key, p.Value);
                            R = await cmd.ExecuteReaderAsync();
                        }
                        t.Commit();
                        while (R.Read()) HS.Add(GetDataRow(R));
                        return await Task.FromResult(HS);
                    }
                    catch (MySqlException ex)
                    {
                        t.Rollback();
                        logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");

                        throw new Exception(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Message}\n{ex.ToString()}");
                throw;
                //return await Task.FromResult(HS);
            }
        }

        public async Task<T> FetchRecordAsync<T>(string sqlString, GetData<T> GetDataRow)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var t = conn.BeginTransaction();
                    try
                    {
                        T DR = default;
                        using (var R = await new MySqlCommand { CommandText = sqlString, Connection = conn, Transaction = t }.ExecuteReaderAsync())
                        {
                            if (R.Read()) DR = GetDataRow(R);
                        }
                        t.Commit();
                        return await Task.FromResult(DR);
                    }
                    catch (MySqlException ex)
                    {
                        t.Rollback();
                        logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");
                        throw new Exception(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Message}\n{ex.ToString()}");
                throw;
                //return await Task.FromResult(DR);
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
        public async Task<int> GetLastInsertedID()
        {
            var sqlString = "SELECT LAST_INSERT_ID();";
            int lastInsertedID = default;
            int.TryParse(await FetchSingleValueAsync(sqlString), out lastInsertedID);
            return lastInsertedID;
        }

        public async Task<(int, long)> AddManyRecordsAsync(string sqlString, ISet<IDictionary<string, object>> sqlParamWithValue)
        {
            return await ExecuteManyCommandsAsync(sqlString, sqlParamWithValue);
        }
        public async Task<long> AddRecordAsync(string sqlString, IDictionary<string, object> sqlParamWithValue)
        {
            return await Task.FromResult(ExecuteCommandAsync(sqlString, sqlParamWithValue).Result.Item2);
        }
        public async Task<int> UpdateRecordAsync(string sqlString, IDictionary<string, object> sqlParameterWithValue)
        {
            return await Task.FromResult(ExecuteCommandAsync(sqlString, sqlParameterWithValue).Result.Item1);
        }
        public async Task<int> UpdateRecordAsync(string sqlString, Tuple<string, object> sqlParameterWithValue)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var T = conn.BeginTransaction();
                using (var cmd = new MySqlCommand { CommandText = sqlString, Connection = conn, Transaction = T })
                {
                    try
                    {
                        cmd.Parameters.AddWithValue(sqlParameterWithValue.Item1, sqlParameterWithValue.Item2);
                        var recordAffected = await cmd.ExecuteNonQueryAsync();
                        T.Commit();
                        return recordAffected;
                    }
                    catch (MySqlException ex)
                    {
                        T.Rollback();
                        logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");
                        return 0;
                    }
                }
            }
        }
        public async Task<int> RemoveRecordAsync(string sqlString, IDictionary<string, object> sqlParameterWithValue)
        {
            return await Task.FromResult(ExecuteCommandAsync(sqlString, sqlParameterWithValue).Result.Item1);
        }

        public async Task<int> RemoveManyRecordsAsync(string sqlString, ISet<IDictionary<string, object>> sqlParameterWithValue)
        {
            return await Task.FromResult(ExecuteManyCommandsAsync(sqlString, sqlParameterWithValue).Result.RecordCount);
        }
        public async Task<int> RemoveManyRecordsAsync(string sqlString, ISet<Tuple<string, object>> sqlParameterWithValue)
        {
            return await ExecuteManyCommandsAsync(sqlString, sqlParameterWithValue);
        }
        async Task<(int RecordCount, long LastInserteId)> ExecuteManyCommandsAsync(string sqlString, ISet<IDictionary<string, object>> sqlParamWithValue)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var T = conn.BeginTransaction();
                using (var cmd = new MySqlCommand { CommandText = sqlString, Connection = conn, Transaction = T })
                {
                    try
                    {
                        cmd.Prepare();
                        int recordAffected = 0;
                        foreach (var p in sqlParamWithValue.First()) cmd.Parameters.AddWithValue(p.Key, p.Value);
                        foreach (var p in sqlParamWithValue) recordAffected += await ExecuteCommandAsync(cmd, p);
                        T.Commit();
                        return (recordAffected, cmd.LastInsertedId);
                    }
                    catch (MySqlException ex)
                    {
                        T.Rollback();
                        //logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");
                        throw new Exception(ex.Message);
                    }
                }
            }
        }
        async Task<int> ExecuteManyCommandsAsync(string sqlString, ISet<Tuple<string, object>> sqlParamWithValue)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var T = conn.BeginTransaction();
                using (var cmd = new MySqlCommand { CommandText = sqlString, Connection = conn, Transaction = T })
                {
                    try
                    {
                        int recordAffected = 0;

                        cmd.Parameters.AddWithValue(sqlParamWithValue.First().Item1, sqlParamWithValue.First().Item2);
                        cmd.Prepare();
                        foreach (var p in sqlParamWithValue) recordAffected += await ExecuteCommandAsync(cmd, p.Item1, p.Item2);
                        T.Commit();
                        return recordAffected;
                    }
                    catch (MySqlException ex)
                    {
                        T.Rollback();
                        //logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");
                        throw new Exception(ex.Message);
                    }
                }
            }
        }

        async Task<(int, long)> ExecuteCommandAsync(string sqlString, IDictionary<string, object> sqlParamWithValue)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var T = conn.BeginTransaction();
                using (var cmd = new MySqlCommand { CommandText = sqlString, Connection = conn, Transaction = T })
                {
                    try
                    {
                        foreach (var p in sqlParamWithValue) cmd.Parameters.AddWithValue(p.Key, p.Value);
                        var recordAffected = await cmd.ExecuteNonQueryAsync();
                        T.Commit();
                        return (recordAffected, cmd.LastInsertedId);
                    }
                    catch (MySqlException ex)
                    {
                        T.Rollback();
                        logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");
                        throw new Exception(ex.Message);
                    }
                }
            }
        }

        async Task<int> ExecuteCommandAsync(MySqlCommand cmd, IDictionary<string, object> paramsWithValues)
        {
            try
            {
                using (cmd)
                {
                    foreach (var p in paramsWithValues) cmd.Parameters[p.Key].Value = p.Value;
                    return await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (MySqlException ex)
            {
                logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");
                throw;
                //switch (ex.Number)
                //{
                //    case 1062:
                //        return 0;
                //    default:
                //        throw;
                //}
            }
        }

        async Task<int> ExecuteCommandAsync(MySqlCommand cmd, string paramName, object paramValue)
        {
            try
            {
                using (cmd)
                {
                    cmd.Parameters[paramName].Value = paramValue;
                    return await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (MySqlException ex)
            {
                logger.Error($"Źródło błedu: {ex.Source};  Kod błędu: {ex.Number} - {ex.Message}\n{ex.ToString()}");
                throw;
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
