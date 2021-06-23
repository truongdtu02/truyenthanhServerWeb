using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace truyenthanhServerWeb.Models
{
    public class TruyenthanhDatabaseSettings : ITruyenthanhDatabaseSettings
    {
        public string AccountCollectionName { get; set; }
        public string DeviceCollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }

    public interface ITruyenthanhDatabaseSettings
    {
        string AccountCollectionName { get; set; }
        string DeviceCollectionName { get; set; }
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
    }
}
