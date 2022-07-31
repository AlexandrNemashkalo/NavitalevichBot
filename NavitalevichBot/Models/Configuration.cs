using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavitalevichBot.Models;

public class Configuration
{
    public string MongoConnectionString { get; set; }
    public string MongoDbName { get; set; }

    public string SqliteMainDbName { get; set; }

    public string SqliteAuthDbName { get; set; }
}
