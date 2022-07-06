using Microsoft.Data.Sqlite;
using System.Data;
using System.Reflection;

namespace NavitalevichBot;

internal class DatabaseContext
{
    private readonly string _connectionString;
    public DatabaseContext()
    {
        var dbName = "sqlite.db";
        var asm = Assembly.GetExecutingAssembly();
        var dbPath = Path.Combine(Path.GetDirectoryName(asm.Location), dbName);

        _connectionString = $"Filename ={ dbPath }";

        if (!File.Exists(dbPath))
        {
            File.WriteAllBytes(dbName, new byte[0]);

            InitializeDatabase();
        }
    }

    public List<string> GetBlackList()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            SELECT *
            FROM BlackList
            ";

            var blackList = new List<string>();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var username = reader.GetString(0);
                    blackList.Add(username);
                }
            }
            return blackList;
        }
    }

    public void AddUserToBlackList(string userName)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            INSERT INTO BlackList(UserName)
            VALUES(@param1)
            ";
            command.Parameters.Add("@param1", SqliteType.Text).Value = userName;
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();
        }
    }

    public void AddMediaToMessage(int messageId, string mediaId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            INSERT INTO MediaToMessage(MessageId, MediaId)
            VALUES(@param1,@param2)
            ";
            command.Parameters.Add("@param1", SqliteType.Integer).Value = messageId;
            command.Parameters.Add("@param2", SqliteType.Text).Value = mediaId;
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();
        }
    }

    public string GetMediaId(int massageId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            SELECT MediaId
            FROM MediaToMessage
            WHERE MessageId = $id
            ";
            command.Parameters.AddWithValue("$id", massageId);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    return reader.GetString(0);
                }
            }
            return null;
        }
    }

    private void InitializeDatabase()
    {
        using (var db = new SqliteConnection(_connectionString))
        {
            db.Open();

            String tableCommand1 = "CREATE TABLE IF NOT EXISTS " +
                "BlackList (UserName NVARCHAR(2048) PRIMARY KEY)";

            SqliteCommand createTable1 = new SqliteCommand(tableCommand1, db);
            createTable1.ExecuteReader();
        }
        using (var db = new SqliteConnection(_connectionString))
        {
            db.Open();

            String tableCommand2 = "CREATE TABLE IF NOT EXISTS " +
                "MediaToMessage (MessageId INTEGER PRIMARY KEY, MediaId NVARCHAR(2048))";

            var createTable2 = new SqliteCommand(tableCommand2, db);
            createTable2.ExecuteReader();
        }
    }
}

