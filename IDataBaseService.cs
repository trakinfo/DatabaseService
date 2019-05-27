using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DataBaseService
{
    public delegate T GetData<T>(IDataReader R);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cmd"></param>
    public delegate void DataParameters(IDbCommand cmd);

    public interface IDataBaseService
    {
        //string ConnectionString { get; }
        //IDbConnection GetConnection();
        bool TestConnection();
        //IDataParameter CreateParameter(string name, DbType type, IDbCommand cmd);
        //IDbCommand CreateCommand(IDbConnection conn, IDbTransaction T, CommandType cmdType, string cmdText);
        Task<IEnumerable<T>> FetchRecordSetAsync<T>(string sqlString, GetData<T> GetDataRow);
        Task<IEnumerable<T>> FetchRecordSetAsync<T>(string sqlString, object[] sqlParameterValue, DataParameters selectParams, GetData<T> GetDataRow);
        Task<T> FetchRecordAsync<T>(string sqlString, GetData<T> GetDataRow);
        Task<string> FetchSingleValueAsync(string sqlString);
        /// <summary>
        /// Adds one record to database
        /// </summary>
        /// <param name="sqlString"> String sql to add data</param>
        /// <param name="sqlParamValue"> Parameters values that are to be added</param>
        /// <param name="createParams"> A callback method to create parameters due to operation</param>
        /// <returns>Record ID that was just added</returns>
        Task<int> AddRecordAsync(string sqlString, object[] sqlParameterValue, DataParameters addParams);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlString"></param>
        /// <param name="sqlParameterValue"></param>
        /// <param name="addParams"></param>
        /// <returns></returns>
        Task<int> AddManyRecordsAsync(string sqlString, ISet<object[]> sqlParameterValue, DataParameters addParams);
        Task<int> UpdateRecordAsync(string sqlString, object[] sqlParameterValue, DataParameters updateParams);
        Task<int> RemoveRecordAsync(string sqlString, object[] sqlParameterValue, DataParameters delParams);
        Task<int> RemoveManyRecordsAsync(string sqlString, ISet<object[]> sqlParameterValue, DataParameters delParams);
    }
}
