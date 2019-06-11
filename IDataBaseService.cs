using System;
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
        Task<IEnumerable<T>> FetchRecordSetAsync<T>(string sqlString, IDictionary<string,object> sqlParameterWithValue, GetData<T> GetDataRow);
        Task<T> FetchRecordAsync<T>(string sqlString, GetData<T> GetDataRow);
        Task<string> FetchSingleValueAsync(string sqlString);
        Task<int> GetLastInsertedID();

        /// <summary>
        /// Adds one record to the database based on a dictionary of parameters with values
        /// </summary>
        /// <param name="sqlString"> Sql insert query to add record to the database</param>
        /// <param name="sqlParameterWithValue">A dictionary of parameters with values</param>
        /// <returns>Record ID which was added</returns>
        Task<int> AddRecordAsync(string sqlString, IDictionary<string,object> sqlParameterWithValue);
        /// <summary>
        /// Adds many records to the database in a single transaction.
        /// </summary>
        /// <param name="sqlString">Sql insert query to add record to the database</param>
        /// <param name="sqlParameterWithValue">Key, value pair that reflects parameter name and parameter value</param>
        /// <returns>Number of records that were added to the database</returns>
        Task<int> AddManyRecordsAsync(string sqlString, ISet<IDictionary<string,object>> sqlParameterWithValue);
        Task<int> UpdateRecordAsync(string sqlString, Tuple<string, object> sqlParameterWithValue);
        Task<int> UpdateRecordAsync(string sqlString, IDictionary<string,object> sqlParameterWithValue);
        /// <summary>
        /// Deletes one record due to passed criteria
        /// </summary>
        /// <param name="sqlString">Sql delete query based on parameters with values</param>
        /// <param name="sqlParameterWithValue">A dictionary containing parameters' names with their values</param>
        /// <returns></returns>
        Task<int> RemoveRecordAsync(string sqlString, IDictionary<string,object> sqlParameterWithValue);
        /// <summary>
        /// Deletes set of records due to passed criteria
        /// </summary>
        /// <param name="sqlString">Sql delete query based on parameters with values</param>
        /// <param name="sqlParameterWithValue">Set of dictionary containing parameters names with their values</param>
        /// <returns></returns>
        Task<int> RemoveManyRecordsAsync(string sqlString, ISet<IDictionary<string,object>> sqlParameterWithValue);
        /// <summary>
        /// Deletes set of records based on only one parameter name and its value
        /// </summary>
        /// <param name="sqlString">Sql delete query based on one parameter with value</param>
        /// <param name="sqlParameterWithValue">Set of Tuple items from which the first is a string and the second is an object</param>
        /// <returns></returns>
        Task<int> RemoveManyRecordsAsync(string sqlString, ISet<Tuple<string, object>> sqlParameterWithValue);
    }
}
