using System.Collections.Generic;

namespace DataBaseService
{
    public class DataBaseParameter : IDataBaseParameter
    {
        public string Name { get; set; }
        public IEnumerable<object> Value { get; set; }
    }
}
