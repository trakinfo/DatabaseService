using System.Collections.Generic;

namespace DataBaseService
{
    public interface IDataBaseParameter
    {
        string Name { get; set; }
        IEnumerable<object> Value { get; set; }
    }
}
