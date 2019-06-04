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
         bool TestConnection();

        /// <summary>
        /// Get many records from database
        /// </summary>
        /// <typeparam name="T">Type of data to return</typeparam>
        /// <param name="sqlString">Sql select query to get records from database</param>
        /// <param name="GetDataRow">Callback method to load data into IEnumerable container</param>
        /// <returns>IEnumerable set of records by pointed type</returns>
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
        /// <param name="sqlString">Sql insert query to add record to database</param>
        /// <param name="sqlParameterValue"></param>
        /// <param name="addParams"></param>
        /// <returns></returns>
        Task<int> AddManyRecordsAsync(string sqlString, ISet<object[]> sqlParameterValue, DataParameters addParams);
        Task<int> AddManyRecordsAsync(string sqlString, ISet<IDictionary<string,object>> sqlParameterWithValue);
        Task<int> UpdateRecordAsync(string sqlString, object[] sqlParameterValue, DataParameters updateParams);
        Task<int> RemoveRecordAsync(string sqlString, object[] sqlParameterValue, DataParameters delParams);
        Task<int> RemoveManyRecordsAsync(string sqlString, ISet<object[]> sqlParameterValue, DataParameters delParams);
    }
}
