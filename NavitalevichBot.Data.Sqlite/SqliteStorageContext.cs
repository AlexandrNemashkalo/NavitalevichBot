using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using NavitalevichBot.Data.Entities;
using System.Data;
using System.Reflection;
using System.Text;

namespace NavitalevichBot.Data.Sqlite;
public class SqliteStorageContext : IStorageContext, IStorageInitializer
{
    private readonly string _dbPathMain;
    private readonly string _dbPathAuth;

    private string _mainConnectionString => $"Filename ={ _dbPathMain }";
    private string _authConnectionString => $"Filename ={ _dbPathAuth }";

    private readonly IConfiguration _config;

    public SqliteStorageContext(IConfiguration config)
    {
        _config = config;

        var asm = Assembly.GetExecutingAssembly();
        _dbPathMain = Path.Combine(Path.GetDirectoryName(asm.Location), _config.GetSection("SqliteMainDbName").Value);
        _dbPathAuth = Path.Combine(Path.GetDirectoryName(asm.Location), _config.GetSection("SqliteAuthDbName").Value);
    }

    public async Task<bool> IsSeenMedia(string mediaId, long chatId)
    {
        using (var connection = new SqliteConnection(_mainConnectionString))
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

    public async Task AddSeenMedia(List<SeenMedia> seenMedias)
    {
        using (var connection = new SqliteConnection(_mainConnectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            var commandText = new StringBuilder(@"
            INSERT INTO SeenMedia(MediaId, ChatId, MessageId)
            VALUES ");

            var i = 1;
            foreach (var seenMedia in seenMedias)
            {
                commandText.Append($"(@mediaId{i}, @chatId{i}), @messageId{i} ");
                command.Parameters.Add($"@mediaId{i}", SqliteType.Text).Value = seenMedia.MediaId;
                command.Parameters.Add($"@chatId{i}", SqliteType.Integer).Value = seenMedia.ChatId;
                command.Parameters.Add($"@messageId{i}", SqliteType.Integer).Value = seenMedia.MessageId;
                i++;
            }
            commandText.Remove(commandText.Length - 2, 2);
            command.CommandText = commandText.ToString();
            command.CommandType = CommandType.Text;
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task AddSeenStories(IEnumerable<long> storyIds, long chatId)
    {
        using (var connection = new SqliteConnection(_mainConnectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            var commandText = new StringBuilder(@"
            INSERT INTO SeenStories(StoryId, ChatId)
            VALUES ");

            var i = 1;
            foreach (var mediaId in storyIds)
            {
                commandText.Append($"(@storyId{i}, @chatId{i}), ");
                command.Parameters.Add($"@storyId{i}", SqliteType.Integer).Value = mediaId;
                command.Parameters.Add($"@chatId{i}", SqliteType.Integer).Value = chatId;
                i++;
            }
            commandText.Remove(commandText.Length - 2, 2);
            command.CommandText = commandText.ToString();
            command.CommandType = CommandType.Text;
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<HashSet<long>> GetUnSeenStories(IEnumerable<long> storyIds, long chatId)
    {
        using (var connection = new SqliteConnection(_mainConnectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            SELECT StoryId
            FROM SeenStories
            WHERE 1=1
                AND ChatId = $chatId
                AND StoryId IN 
            ";

            command.CommandText = command.CommandText + $" ({string.Join(", ", storyIds)})";
            command.Parameters.AddWithValue("$chatId", chatId);

            var seenStories = new HashSet<long>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    var storyId = reader.GetString(0);
                    seenStories.Add(long.Parse(storyId));
                }
            }

            var result = storyIds.Except(seenStories);
            return result.ToHashSet();
        }
    }

    public async Task<List<string>> GetBlackList(long chatId)
    {
        using (var connection = new SqliteConnection(_mainConnectionString))
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
        using (var connection = new SqliteConnection(_mainConnectionString))
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

    public async Task<string> GetMediaIdByMessageId(int messageId, long chatId)
    {
        using (var connection = new SqliteConnection(_mainConnectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            SELECT MediaId
            FROM SeenMedia
            WHERE 1=1
                AND MessageId = $messageId
                AND ChatId = $chatId
            ";
            command.Parameters.AddWithValue("$messageId", messageId);
            command.Parameters.AddWithValue("$chatId", chatId);


            var result = await command.ExecuteScalarAsync();
            return (string)result;
        }
    }

    public async Task AddAvaliableChatId(long chatId, string name)
    {
        using (var connection = new SqliteConnection(_authConnectionString))
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
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<bool> IsAvaliableChatId(long chatId)
    {
        using (var connection = new SqliteConnection(_authConnectionString))
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

    public async Task AddSessionMessage(int messageId, long chatId)
    {
        using (var connection = new SqliteConnection(_authConnectionString))
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
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<int?> GetSessionMessage(long chatId)
    {
        using (var connection = new SqliteConnection(_authConnectionString))
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

            using (var reader = await command.ExecuteReaderAsync())
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

    public async Task DeleteSessionMessage(long chatId)
    {
        using (var connection = new SqliteConnection(_authConnectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            Delete FROM SessionMessages
            WHERE 1=1
                AND ChatId = $chatId
            ";
            command.Parameters.AddWithValue("$chatId", chatId);

            await command.ExecuteNonQueryAsync();
        }
    }

    private void InitializeDatabase()
    {
        using (var db = new SqliteConnection(_mainConnectionString))
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

        using (var db = new SqliteConnection(_mainConnectionString))
        {
            db.Open();

            var seenMedia = @"
                CREATE TABLE IF NOT EXISTS SeenMedia
                (
                    MediaId NVARCHAR(2048),
                    ChatId INTEGER,
                    MessageId INTEGER,
                    PRIMARY KEY (MediaId, ChatId)
                )";

            var createTable3 = new SqliteCommand(seenMedia, db);
            createTable3.ExecuteReader();
        }

        using (var db = new SqliteConnection(_mainConnectionString))
        {
            db.Open();

            var seenStories = @"
                CREATE TABLE IF NOT EXISTS SeenStories
                (
                    StoryId INTEGER,
                    ChatId INTEGER,
                    PRIMARY KEY (StoryId, ChatId)
                )";

            var createTable4 = new SqliteCommand(seenStories, db);
            createTable4.ExecuteReader();
        }

        using (var db = new SqliteConnection(_authConnectionString))
        {
            db.Open();

            var tableCommand5 = @"
                CREATE TABLE IF NOT EXISTS AvaliableChats
                (
                    ChatId INTEGER PRIMARY KEY,
                    Name NVARCHAR(2048)
                )";

            var createTable5 = new SqliteCommand(tableCommand5, db);
            createTable5.ExecuteReader();
        }

        var task = IsAvaliableChatId(long.Parse(_config.GetSection("AdminChatId").Value));
        task.Wait();
        if (!task.Result)
        {
            var addAvaliableChatIdTask = AddAvaliableChatId(long.Parse(_config.GetSection("AdminChatId").Value), _config.GetSection("AdminName").Value);
            addAvaliableChatIdTask.Wait();
        }

        using (var db = new SqliteConnection(_authConnectionString))
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

    public async Task InitializeStorage()
    {
        if (!File.Exists(_dbPathMain))
        {
            File.WriteAllBytes(_dbPathMain, new byte[0]);
        }
        if (!File.Exists(_dbPathAuth))
        {
            File.WriteAllBytes(_dbPathAuth, new byte[0]);
        }

        using (var db = new SqliteConnection(_mainConnectionString))
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
            await createTable1.ExecuteReaderAsync();
        }

        using (var db = new SqliteConnection(_mainConnectionString))
        {
            db.Open();

            var seenMedia = @"
                CREATE TABLE IF NOT EXISTS SeenMedia
                (
                    MediaId NVARCHAR(2048),
                    ChatId INTEGER,
                    MessageId INTEGER,
                    PRIMARY KEY (MediaId, ChatId)
                )";

            var createTable3 = new SqliteCommand(seenMedia, db);
            await createTable3.ExecuteReaderAsync();
        }

        using (var db = new SqliteConnection(_mainConnectionString))
        {
            db.Open();

            var seenStories = @"
                CREATE TABLE IF NOT EXISTS SeenStories
                (
                    StoryId INTEGER,
                    ChatId INTEGER,
                    PRIMARY KEY (StoryId, ChatId)
                )";

            var createTable4 = new SqliteCommand(seenStories, db);
            await createTable4.ExecuteReaderAsync();
        }

        using (var db = new SqliteConnection(_authConnectionString))
        {
            db.Open();

            var tableCommand5 = @"
                CREATE TABLE IF NOT EXISTS AvaliableChats
                (
                    ChatId INTEGER PRIMARY KEY,
                    Name NVARCHAR(2048)
                )";

            var createTable5 = new SqliteCommand(tableCommand5, db);
            await createTable5.ExecuteReaderAsync();
        }

        var isAvaliable = await IsAvaliableChatId(long.Parse(_config.GetSection("AdminChatId").Value));
        if (!isAvaliable)
        {
            await AddAvaliableChatId(long.Parse(_config.GetSection("AdminChatId").Value), _config.GetSection("AdminName").Value);
        }

        using (var db = new SqliteConnection(_authConnectionString))
        {
            db.Open();

            var tableCommand5 = @"
                CREATE TABLE IF NOT EXISTS SessionMessages
                (
                    ChatId INTEGER PRIMARY KEY ,
                    MessageId INTEGER
                )";

            var createTable5 = new SqliteCommand(tableCommand5, db);
            await createTable5.ExecuteReaderAsync();
        }
    }
}
