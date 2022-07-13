using Microsoft.Data.Sqlite;
using System.Data;
using System.Reflection;
using System.Text;

namespace NavitalevichBot;

internal class DatabaseContext
{
    private readonly string _connectionString;
    public DatabaseContext()
    {
        var dbName = "navitalevichbot.db";
        var asm = Assembly.GetExecutingAssembly();
        var dbPath = Path.Combine(Path.GetDirectoryName(asm.Location), dbName);

        _connectionString = $"Filename ={ dbPath }";

        if (!File.Exists(dbPath))
        {
            File.WriteAllBytes(dbName, new byte[0]);
        }
        InitializeDatabase();
    }

    public async Task<bool> IsSeenMedia(string mediaId, long chatId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            SELECT *
            FROM SeenMedia
            WHERE 1=1
                AND ChatId = $chatId
                AND MediaId = $mediaId
            ";

            command.Parameters.AddWithValue("$chatId", chatId);
            command.Parameters.AddWithValue("$mediaId", mediaId);

            var result = await command.ExecuteScalarAsync();
            return result != null;
        }
    }

    public async Task AddSeenMedia(List<string> mediaIds, long chatId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            var commandText = new StringBuilder(@"
            INSERT INTO SeenMedia(MediaId, ChatId)
            VALUES ");

            var i = 1;
            foreach (var mediaId in mediaIds)
            {
                commandText.Append($"(@mediaId{i}, @chatId{i}), ");
                command.Parameters.Add($"@mediaId{i}", SqliteType.Text).Value = mediaId;
                command.Parameters.Add($"@chatId{i}", SqliteType.Integer).Value = chatId;
                i++;
            }
            commandText.Remove(commandText.Length - 2, 2);
            command.CommandText = commandText.ToString();
            command.CommandType = CommandType.Text;
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<List<string>> GetBlackList(long chatId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            SELECT UserName
            FROM BlackList
            WHERE 1=1
                AND ChatId = $chatId
            ";
            command.Parameters.AddWithValue("$chatId", chatId);

            var blackList = new List<string>();
            using (var reader = await command.ExecuteReaderAsync())
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

    public async Task AddUserToBlackList(string userName, long chatId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            INSERT INTO BlackList(UserName, ChatId)
            VALUES(@userName,@chatId)
            ";
            command.Parameters.Add("@userName", SqliteType.Text).Value = userName;
            command.Parameters.Add("@chatId", SqliteType.Integer).Value = chatId;
            command.CommandType = CommandType.Text;
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task AddMediaToMessage(int messageId, string mediaId, long chatId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            INSERT INTO MessageToMedia(MessageId, MediaId, ChatId)
            VALUES(@messageId,@mediaId, @chatId)
            ";
            command.Parameters.Add("@messageId", SqliteType.Integer).Value = messageId;
            command.Parameters.Add("@mediaId", SqliteType.Text).Value = mediaId;
            command.Parameters.Add("@chatId", SqliteType.Integer).Value = chatId;
            command.CommandTimeout = 2; 
            command.CommandType = CommandType.Text;
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<string> GetMediaIdByMessageId(int messageId, long chatId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            SELECT MediaId
            FROM MessageToMedia
            WHERE 1=1
                AND MessageId = $messageId
                AND ChatId = $chatId
            ";
            command.Parameters.AddWithValue("$messageId", messageId);
            command.Parameters.AddWithValue("$chatId", chatId);


            var result = await command.ExecuteScalarAsync();
            return (string) result;
        }
    }

    public void AddAvaliableChatId(long chatId, string name)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            INSERT INTO AvaliableChats(ChatId, Name)
            VALUES(@chatId,@name)
            ";
          
            command.Parameters.Add("@chatId", SqliteType.Integer).Value = chatId;
            command.Parameters.Add("@name", SqliteType.Text).Value = name;
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();
        }
    }

    public async Task<bool> IsAvaliableChatId(long chatId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            SELECT *
            FROM AvaliableChats
            WHERE 1=1
                AND ChatId = $chatId
            ";
            command.Parameters.AddWithValue("$chatId", chatId);

            var result = await command.ExecuteScalarAsync();

            return result != null;
        }
    }

    public void AddSessionMessage(int messageId, long chatId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            INSERT INTO SessionMessages(ChatId, MessageId)
            VALUES(@chatId,@messageId)
            ";

            command.Parameters.Add("@chatId", SqliteType.Integer).Value = chatId;
            command.Parameters.Add("@messageId", SqliteType.Integer).Value = messageId;
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();
        }
    }

    public int? GetSessionMessage(long chatId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            SELECT MessageId
            FROM SessionMessages
            WHERE 1=1
                AND ChatId = $chatId
            ";
            command.Parameters.AddWithValue("$chatId", chatId);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var messageId = reader.GetString(0);
                    return int.Parse(messageId);
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

            var tableCommand1 = @"
                CREATE TABLE IF NOT EXISTS BlackList
                (
                    UserName NVARCHAR(2048), 
                    ChatId INTEGER,
                    PRIMARY KEY (UserName, ChatId)
                )";

            SqliteCommand createTable1 = new SqliteCommand(tableCommand1, db);
            createTable1.ExecuteReader();
        }
        using (var db = new SqliteConnection(_connectionString))
        {
            db.Open();

            var tableCommand2 = @"
                CREATE TABLE IF NOT EXISTS MessageToMedia
                (
                    MessageId INTEGER,
                    ChatId INTEGER,
                    MediaId NVARCHAR(2048),
                    PRIMARY KEY (MessageId, ChatId)
                )";

            var createTable2 = new SqliteCommand(tableCommand2, db);
            createTable2.ExecuteReader();
        }

        using (var db = new SqliteConnection(_connectionString))
        {
            db.Open();

            var seenMedia = @"
                CREATE TABLE IF NOT EXISTS SeenMedia
                (
                    MediaId NVARCHAR(2048),
                    ChatId INTEGER,
                    PRIMARY KEY (MediaId, ChatId)
                )";

            var createTable3 = new SqliteCommand(seenMedia, db);
            createTable3.ExecuteReader();
        }

        using (var db = new SqliteConnection(_connectionString))
        {
            db.Open();

            var tableCommand4 = @"
                CREATE TABLE IF NOT EXISTS AvaliableChats
                (
                    ChatId INTEGER PRIMARY KEY,
                    Name NVARCHAR(2048)
                )";

            var createTable4 = new SqliteCommand(tableCommand4, db);
            createTable4.ExecuteReader();
        }

        var task = IsAvaliableChatId(Constants.AdminChatId);
        task.Wait();
        if (!task.Result)
        {
            AddAvaliableChatId(Constants.AdminChatId, Constants.AdminName);
        }

        using (var db = new SqliteConnection(_connectionString))
        {
            db.Open();

            var tableCommand5 = @"
                CREATE TABLE IF NOT EXISTS SessionMessages
                (
                    ChatId INTEGER PRIMARY KEY ,
                    MessageId INTEGER
                )";

            var createTable5 = new SqliteCommand(tableCommand5, db);
            createTable5.ExecuteReader();
        }
    }
}

