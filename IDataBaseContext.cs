using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBaseContext
{
    public delegate T GetData<T>(IDataReader R);
    public delegate void DataParameters(IDbCommand cmd);

    public interface IDataBaseContext
    {
        string ConnectionString { get; }
        IDbConnection GetConnection();
        IDataParameter CreateParameter(string name, DbType type, IDbCommand cmd);
        IDbCommand CreateCommand(IDbConnection conn, IDbTransaction T, CommandType cmdType, string cmdText);
        Task<IEnumerable<T>> FetchRecordSetAsync<T>(string sqlString, GetData<T> GetDataRow);
        Task<IEnumerable<T>> FetchRecordSetAsync<T>(string sqlString, object[] sqlParameterValue, DataParameters selectParams, GetData<T> GetDataRow);
        Task<T> FetchRecordAsync<T>(string sqlString, GetData<T> GetDataRow);
        Task AddRecordAsync(string sqlString, object[] sqlParameterValue, DataParameters addParams);
        Task<int> AddManyRecordsAsync(string sqlString, ISet<object[]> sqlParameterValue, DataParameters addParams);
        Task<int> UpdateRecordAsync(string sqlString, object[] sqlParameterValue, DataParameters updateParams);
        Task<int> RemoveRecordAsync(string sqlString, object[] sqlParameterValue, DataParameters delParams);
    }
}
