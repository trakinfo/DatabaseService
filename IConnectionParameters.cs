using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBaseService
{
    public interface IConnectionParameters
    {
        string ServerAddress { get; set; }
        string DBName { get; set; }
        string UserName { get; set; }
        string Password { get; set; }
        int SSLMode { get; set; }
        string CharSet { get; set; }
        int KeepAlive { get; set; } 
        uint ServerPort { get; set; } 
    }
}
