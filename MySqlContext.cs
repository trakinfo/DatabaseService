using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DataBaseContext
{
    public class MySqlContext : IDataBaseContext
    {
        public string ConnectionString { get; }

        public IDbConnection GetConnection()
        {
            IDbConnection dbConn = new MySqlConnection(ConnectionString);
            return dbConn;
        }
        public MySqlContext(string connectionString)
        {
            ConnectionString = connectionString;
        }
        public async Task<IEnumerable<T>> FetchRecordSetAsync<T>(string sqlString, GetData<T> GetDataRow)
        {
            var HS = new HashSet<T>();
            try
            {
                using (var conn = (MySqlConnection)GetConnection())
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
                        Console.WriteLine(ex.Message);
                        t.Rollback();
                    }
                }
                return await Task.FromResult(HS);
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe.Message);
                return await Task.FromResult(HS);
            }
        }

        public async Task<IEnumerable<T>> FetchRecordSetAsync<T>(string sqlString, object[] sqlParameterValue, DataParameters createParams, GetData<T> GetDataRow)
        {
            var HS = new HashSet<T>();
            try
            {
                using (var conn = (MySqlConnection)GetConnection())
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
                        Console.WriteLine(ex.Message);
                        t.Rollback();
                    }
                }
                return await Task.FromResult(HS);
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe.Message);
                return await Task.FromResult(HS);
            }
        }

        public async Task<T> FetchRecordAsync<T>(string sqlString, GetData<T> GetDataRow)
        {
            T DR = default(T);
            try
            {
                using (var conn = (MySqlConnection)GetConnection())
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
                        Console.WriteLine(ex.Message);
                        t.Rollback();
                    }
                }
                return await Task.FromResult(DR);
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe.Message);
                return await Task.FromResult(DR);
            }
        }
        public async Task<int> AddManyRecordsAsync(string sqlString, ISet<object[]> sqlParamValue, DataParameters createParams)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var T = conn.BeginTransaction();
                var cmd = CreateCommand(conn, T, CommandType.Text, sqlString);
                createParams(cmd);
                int recordAffected = 0;
                try
                {
                    foreach (var p in sqlParamValue) recordAffected += await ExecuteCommandAsync(cmd, p);
                    T.Commit();
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine(ex.Message);
                    T.Rollback();
                    //return 0;
                }
                return recordAffected;
            }
        }

        public async Task AddRecordAsync(string sqlString, object[] sqlParamValue, DataParameters createParams)
        {
            await ExecuteCommandAsync(sqlString, sqlParamValue, createParams);
        }

        public async Task<int> UpdateRecordAsync(string sqlString, object[] sqlParameterValue, DataParameters updateParams)
        {
            return await ExecuteCommandAsync(sqlString, sqlParameterValue, updateParams);
        }

        public async Task<int> RemoveRecordAsync(string sqlString, object[] sqlParameterValue, DataParameters delParams)
        {
            return await ExecuteCommandAsync(sqlString, sqlParameterValue, delParams);
        }

        async Task<int> ExecuteCommandAsync(string sqlString, object[] sqlParamValue, DataParameters createParams)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var T = conn.BeginTransaction();
                var cmd = CreateCommand(conn, T, CommandType.Text, sqlString);
                createParams(cmd);
                try
                {
                    var recordAffected = await ExecuteCommandAsync(cmd, sqlParamValue);
                    T.Commit();
                    return recordAffected;
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine(ex.Message);
                    T.Rollback();
                    return 0;
                }
            }
        }

        async Task<int> ExecuteCommandAsync(IDbCommand cmd, object[] sqlParamValue)
        {
            using (cmd)
            {
                int count = 0;
                try
                {
                    for (int i = 0; i < sqlParamValue.Length; i++)
                        ((MySqlCommand)cmd).Parameters[i].Value = sqlParamValue[i];
                    count = await ((MySqlCommand)cmd).ExecuteNonQueryAsync();
                }
                catch (MySqlException ex)
                {
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
        public IDataParameter CreateParameter(string name, DbType type, IDbCommand cmd)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.DbType = type;

            return p;
        }

        public IDbCommand CreateCommand(IDbConnection conn, IDbTransaction T, CommandType cmdType, string cmdText)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandType = cmdType;
            cmd.CommandText = cmdText;
            cmd.Transaction = T;
            return cmd;
        }
    }
}
