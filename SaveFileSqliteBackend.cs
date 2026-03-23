/*
@file SaveFileSqliteBackend.cs
@description Internal SQLite backend for PaxDrops save snapshots, including schema setup, runtime validation, and save-folder inspection.
@editCount 1
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using PaxDrops.MrStacks;
using SQLitePCL;

namespace PaxDrops
{
    internal static class SaveFileSqliteBackend
    {
        private const int SchemaVersion = 1;
        private const string DatabaseFileName = "paxdrops.db";
        private static bool _initialized;

        internal sealed class Snapshot
        {
            public SaveFileJsonDataStore.SaveMetadata? Metadata { get; set; }
            public int LastMrStacksOrderDay { get; set; } = -1;
            public Dictionary<int, List<SaveFileJsonDataStore.DropRecord>> PendingDrops { get; } = new();
            public Dictionary<int, int> MrStacksOrdersToday { get; } = new();
            public List<MrStacksMessaging.MessageRecord> ConversationMessages { get; } = new();
        }

        internal sealed class SaveDirectoryInspection
        {
            public string SaveId { get; set; } = "";
            public string DirectoryPath { get; set; } = "";
            public string DatabasePath { get; set; } = "";
            public bool HasDatabase { get; set; }
            public int FileCount { get; set; }
            public long TotalSize { get; set; }
            public long DatabaseSize { get; set; }
            public DateTime LastModified { get; set; }
            public int DropCount { get; set; }
            public int OrderRecordCount { get; set; }
            public int ConversationCount { get; set; }
            public int LastMrStacksOrderDay { get; set; } = -1;
            public int LegacyJsonFileCount { get; set; }
            public Dictionary<string, long> FileSizes { get; } = new();
            public SaveFileJsonDataStore.SaveMetadata? Metadata { get; set; }
            public string? InspectionError { get; set; }
        }

        internal static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                Batteries_V2.Init();
            }
            catch (Exception batteryEx)
            {
                Logger.Warn(
                    $"SQLite batteries init failed: {batteryEx.Message}. Retrying with direct e_sqlite3 provider setup.",
                    "SaveFileSqliteBackend");
                raw.SetProvider(new SQLite3Provider_e_sqlite3());
            }

            VerifyRuntime();
            _initialized = true;
            Logger.Debug("SQLite backend initialized and runtime probe succeeded", "SaveFileSqliteBackend");
        }

        internal static string GetDatabaseFileName()
        {
            return DatabaseFileName;
        }

        internal static string GetDatabasePath(string saveDirectoryPath)
        {
            return Path.Combine(saveDirectoryPath, DatabaseFileName);
        }

        internal static bool DatabaseExists(string saveDirectoryPath)
        {
            return File.Exists(GetDatabasePath(saveDirectoryPath));
        }

        internal static void EnsureDatabase(string dbPath)
        {
            string? directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var connection = OpenConnection(dbPath);
            ApplySchema(connection);
        }

        internal static Snapshot LoadSnapshot(string dbPath)
        {
            EnsureDatabase(dbPath);

            using var connection = OpenConnection(dbPath);
            ApplySchema(connection);

            var snapshot = new Snapshot
            {
                Metadata = LoadMetadata(connection),
                LastMrStacksOrderDay = LoadLastOrderDay(connection)
            };

            LoadDrops(connection, snapshot.PendingDrops);
            LoadOrders(connection, snapshot.MrStacksOrdersToday);
            LoadConversationMessages(connection, snapshot.ConversationMessages);

            return snapshot;
        }

        internal static void SaveSnapshot(
            string dbPath,
            SaveFileJsonDataStore.SaveMetadata metadata,
            int lastMrStacksOrderDay,
            IReadOnlyDictionary<int, List<SaveFileJsonDataStore.DropRecord>> pendingDrops,
            IReadOnlyDictionary<int, int> mrStacksOrdersToday,
            IReadOnlyList<MrStacksMessaging.MessageRecord> conversationMessages)
        {
            string? directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var connection = OpenConnection(dbPath);
            ApplySchema(connection);

            using var transaction = connection.BeginTransaction();

            UpsertMetadata(connection, transaction, metadata, lastMrStacksOrderDay);
            ReplaceDrops(connection, transaction, pendingDrops);
            ReplaceOrders(connection, transaction, mrStacksOrdersToday);
            ReplaceConversationMessages(connection, transaction, conversationMessages);

            transaction.Commit();
        }

        internal static List<MrStacksMessaging.MessageRecord> LoadConversationMessages(string dbPath)
        {
            EnsureDatabase(dbPath);

            using var connection = OpenConnection(dbPath);
            ApplySchema(connection);

            var messages = new List<MrStacksMessaging.MessageRecord>();
            LoadConversationMessages(connection, messages);
            return messages;
        }

        internal static void SaveConversationMessages(string dbPath, IReadOnlyList<MrStacksMessaging.MessageRecord> messages)
        {
            string? directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var connection = OpenConnection(dbPath);
            ApplySchema(connection);
            using var transaction = connection.BeginTransaction();
            ReplaceConversationMessages(connection, transaction, messages);
            transaction.Commit();
        }

        internal static SaveDirectoryInspection InspectSaveDirectory(string directoryPath)
        {
            var inspection = new SaveDirectoryInspection
            {
                SaveId = Path.GetFileName(directoryPath),
                DirectoryPath = directoryPath,
                DatabasePath = GetDatabasePath(directoryPath),
                LastModified = Directory.Exists(directoryPath)
                    ? Directory.GetLastWriteTime(directoryPath)
                    : DateTime.MinValue
            };

            try
            {
                if (Directory.Exists(directoryPath))
                {
                    foreach (string jsonFile in Directory.GetFiles(directoryPath, "*.json"))
                    {
                        var fileInfo = new FileInfo(jsonFile);
                        inspection.LegacyJsonFileCount++;
                        inspection.TotalSize += fileInfo.Length;
                        inspection.FileSizes[Path.GetFileName(jsonFile)] = fileInfo.Length;
                        if (fileInfo.LastWriteTime > inspection.LastModified)
                        {
                            inspection.LastModified = fileInfo.LastWriteTime;
                        }
                    }
                }

                if (!File.Exists(inspection.DatabasePath))
                {
                    inspection.FileCount = inspection.LegacyJsonFileCount;
                    return inspection;
                }

                inspection.HasDatabase = true;

                var dbInfo = new FileInfo(inspection.DatabasePath);
                inspection.DatabaseSize = dbInfo.Length;
                inspection.TotalSize += dbInfo.Length;
                inspection.FileCount = inspection.LegacyJsonFileCount + 1;
                inspection.FileSizes[DatabaseFileName] = dbInfo.Length;
                if (dbInfo.LastWriteTime > inspection.LastModified)
                {
                    inspection.LastModified = dbInfo.LastWriteTime;
                }

                using var connection = OpenConnection(inspection.DatabasePath, readOnly: true);
                inspection.Metadata = LoadMetadata(connection);
                inspection.LastMrStacksOrderDay = LoadLastOrderDay(connection);
                inspection.DropCount = CountRows(connection, "drops");
                inspection.OrderRecordCount = CountRows(connection, "mr_stacks_daily_orders");
                inspection.ConversationCount = CountRows(connection, "conversation_messages");
            }
            catch (Exception ex)
            {
                inspection.InspectionError = ex.Message;
            }

            return inspection;
        }

        private static SqliteConnection OpenConnection(string dbPath, bool readOnly = false)
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate
            };

            var connection = new SqliteConnection(builder.ToString());
            connection.Open();

            using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();

            return connection;
        }

        private static void VerifyRuntime()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT sqlite_version();";
            _ = command.ExecuteScalar();
        }

        private static void ApplySchema(SqliteConnection connection)
        {
            int currentVersion = GetUserVersion(connection);

            if (currentVersion == SchemaVersion)
            {
                return;
            }

            const string schemaSql = @"
CREATE TABLE IF NOT EXISTS save_metadata (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    save_id TEXT NOT NULL,
    steam_id TEXT NOT NULL,
    save_name TEXT NOT NULL,
    save_path TEXT NOT NULL,
    organization_name TEXT NOT NULL,
    start_date TEXT NOT NULL,
    creation_timestamp TEXT NOT NULL,
    last_accessed TEXT NOT NULL,
    last_mr_stacks_order_day INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS drops (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    day INTEGER NOT NULL,
    drop_hour INTEGER NOT NULL,
    drop_time TEXT NOT NULL,
    org TEXT NOT NULL,
    created_time TEXT NOT NULL,
    type TEXT NOT NULL,
    location TEXT NOT NULL,
    expiry_time TEXT NOT NULL,
    order_day INTEGER NOT NULL,
    is_collected INTEGER NOT NULL,
    initial_item_count INTEGER NOT NULL,
    ordered_tier_id TEXT NOT NULL,
    ordered_tier_name TEXT NOT NULL,
    price_paid INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS drop_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    drop_id INTEGER NOT NULL,
    ordinal INTEGER NOT NULL,
    item_id TEXT NOT NULL,
    amount INTEGER NOT NULL,
    FOREIGN KEY(drop_id) REFERENCES drops(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS mr_stacks_daily_orders (
    day INTEGER PRIMARY KEY,
    order_count INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS conversation_messages (
    seq INTEGER PRIMARY KEY AUTOINCREMENT,
    message_id TEXT NOT NULL,
    text TEXT NOT NULL,
    timestamp TEXT NOT NULL,
    game_day INTEGER NOT NULL,
    game_time INTEGER NOT NULL,
    is_from_player INTEGER NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_drops_day ON drops(day);
CREATE INDEX IF NOT EXISTS idx_drop_items_drop_id ON drop_items(drop_id);
";

            using var command = connection.CreateCommand();
            command.CommandText = schemaSql;
            command.ExecuteNonQuery();

            using var versionCommand = connection.CreateCommand();
            versionCommand.CommandText = $"PRAGMA user_version = {SchemaVersion};";
            versionCommand.ExecuteNonQuery();
        }

        private static int GetUserVersion(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version;";
            object? result = command.ExecuteScalar();
            return Convert.ToInt32(result ?? 0);
        }

        private static SaveFileJsonDataStore.SaveMetadata? LoadMetadata(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT save_id, steam_id, save_name, save_path, organization_name, start_date, creation_timestamp, last_accessed
FROM save_metadata
WHERE id = 1;";

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new SaveFileJsonDataStore.SaveMetadata
            {
                SaveId = GetString(reader, 0),
                SteamId = GetString(reader, 1),
                SaveName = GetString(reader, 2),
                SavePath = GetString(reader, 3),
                OrganizationName = GetString(reader, 4),
                StartDate = GetString(reader, 5),
                CreationTimestamp = GetString(reader, 6),
                LastAccessed = GetString(reader, 7)
            };
        }

        private static int LoadLastOrderDay(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT last_mr_stacks_order_day FROM save_metadata WHERE id = 1;";
            object? result = command.ExecuteScalar();
            return result == null || result == DBNull.Value ? -1 : Convert.ToInt32(result);
        }

        private static void LoadDrops(
            SqliteConnection connection,
            IDictionary<int, List<SaveFileJsonDataStore.DropRecord>> pendingDrops)
        {
            pendingDrops.Clear();

            var dropIds = new Dictionary<long, SaveFileJsonDataStore.DropRecord>();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT id, day, drop_hour, drop_time, org, created_time, type, location, expiry_time, order_day,
       is_collected, initial_item_count, ordered_tier_id, ordered_tier_name, price_paid
FROM drops
ORDER BY day, id;";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    long dropId = reader.GetInt64(0);
                    var record = new SaveFileJsonDataStore.DropRecord
                    {
                        Day = reader.GetInt32(1),
                        DropHour = reader.GetInt32(2),
                        DropTime = GetString(reader, 3),
                        Org = GetString(reader, 4),
                        CreatedTime = GetString(reader, 5),
                        Type = GetString(reader, 6),
                        Location = GetString(reader, 7),
                        ExpiryTime = GetString(reader, 8),
                        OrderDay = reader.GetInt32(9),
                        IsCollected = reader.GetInt32(10) != 0,
                        InitialItemCount = reader.GetInt32(11),
                        OrderedTierId = GetString(reader, 12),
                        OrderedTierName = GetString(reader, 13),
                        PricePaid = reader.GetInt32(14)
                    };

                    if (!pendingDrops.TryGetValue(record.Day, out List<SaveFileJsonDataStore.DropRecord>? dayDrops))
                    {
                        dayDrops = new List<SaveFileJsonDataStore.DropRecord>();
                        pendingDrops[record.Day] = dayDrops;
                    }

                    dayDrops.Add(record);
                    dropIds[dropId] = record;
                }
            }

            using var itemsCommand = connection.CreateCommand();
            itemsCommand.CommandText = @"
SELECT drop_id, ordinal, item_id, amount
FROM drop_items
ORDER BY drop_id, ordinal;";

            using var itemsReader = itemsCommand.ExecuteReader();
            while (itemsReader.Read())
            {
                long dropId = itemsReader.GetInt64(0);
                if (!dropIds.TryGetValue(dropId, out SaveFileJsonDataStore.DropRecord? record))
                {
                    continue;
                }

                string itemId = GetString(itemsReader, 2);
                int amount = itemsReader.GetInt32(3);
                record.Items.Add(FormatItemEntry(itemId, amount));
            }
        }

        private static void LoadOrders(SqliteConnection connection, IDictionary<int, int> mrStacksOrdersToday)
        {
            mrStacksOrdersToday.Clear();

            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT day, order_count
FROM mr_stacks_daily_orders
ORDER BY day;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                mrStacksOrdersToday[reader.GetInt32(0)] = reader.GetInt32(1);
            }
        }

        private static void LoadConversationMessages(
            SqliteConnection connection,
            ICollection<MrStacksMessaging.MessageRecord> messages)
        {
            messages.Clear();

            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT message_id, text, timestamp, game_day, game_time, is_from_player
FROM conversation_messages
ORDER BY seq;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                messages.Add(new MrStacksMessaging.MessageRecord
                {
                    MessageId = GetString(reader, 0),
                    Text = GetString(reader, 1),
                    Timestamp = GetString(reader, 2),
                    GameDay = reader.GetInt32(3),
                    GameTime = reader.GetInt32(4),
                    IsFromPlayer = reader.GetInt32(5) != 0
                });
            }
        }

        private static void UpsertMetadata(
            SqliteConnection connection,
            SqliteTransaction transaction,
            SaveFileJsonDataStore.SaveMetadata metadata,
            int lastMrStacksOrderDay)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO save_metadata (
    id, save_id, steam_id, save_name, save_path, organization_name, start_date,
    creation_timestamp, last_accessed, last_mr_stacks_order_day
) VALUES (
    1, @saveId, @steamId, @saveName, @savePath, @organizationName, @startDate,
    @creationTimestamp, @lastAccessed, @lastMrStacksOrderDay
)
ON CONFLICT(id) DO UPDATE SET
    save_id = excluded.save_id,
    steam_id = excluded.steam_id,
    save_name = excluded.save_name,
    save_path = excluded.save_path,
    organization_name = excluded.organization_name,
    start_date = excluded.start_date,
    creation_timestamp = excluded.creation_timestamp,
    last_accessed = excluded.last_accessed,
    last_mr_stacks_order_day = excluded.last_mr_stacks_order_day;";
            command.Parameters.AddWithValue("@saveId", metadata.SaveId);
            command.Parameters.AddWithValue("@steamId", metadata.SteamId);
            command.Parameters.AddWithValue("@saveName", metadata.SaveName);
            command.Parameters.AddWithValue("@savePath", metadata.SavePath);
            command.Parameters.AddWithValue("@organizationName", metadata.OrganizationName);
            command.Parameters.AddWithValue("@startDate", metadata.StartDate);
            command.Parameters.AddWithValue("@creationTimestamp", metadata.CreationTimestamp);
            command.Parameters.AddWithValue("@lastAccessed", metadata.LastAccessed);
            command.Parameters.AddWithValue("@lastMrStacksOrderDay", lastMrStacksOrderDay);
            command.ExecuteNonQuery();
        }

        private static void ReplaceDrops(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyDictionary<int, List<SaveFileJsonDataStore.DropRecord>> pendingDrops)
        {
            ExecuteDelete(connection, transaction, "DELETE FROM drop_items;");
            ExecuteDelete(connection, transaction, "DELETE FROM drops;");

            foreach ((int _, List<SaveFileJsonDataStore.DropRecord> dayDrops) in pendingDrops.OrderBy(kvp => kvp.Key))
            {
                foreach (SaveFileJsonDataStore.DropRecord drop in dayDrops)
                {
                    long dropId = InsertDrop(connection, transaction, drop);
                    InsertDropItems(connection, transaction, dropId, drop.Items);
                }
            }
        }

        private static long InsertDrop(
            SqliteConnection connection,
            SqliteTransaction transaction,
            SaveFileJsonDataStore.DropRecord drop)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO drops (
    day, drop_hour, drop_time, org, created_time, type, location, expiry_time,
    order_day, is_collected, initial_item_count, ordered_tier_id, ordered_tier_name, price_paid
) VALUES (
    @day, @dropHour, @dropTime, @org, @createdTime, @type, @location, @expiryTime,
    @orderDay, @isCollected, @initialItemCount, @orderedTierId, @orderedTierName, @pricePaid
);";
            command.Parameters.AddWithValue("@day", drop.Day);
            command.Parameters.AddWithValue("@dropHour", drop.DropHour);
            command.Parameters.AddWithValue("@dropTime", drop.DropTime);
            command.Parameters.AddWithValue("@org", drop.Org);
            command.Parameters.AddWithValue("@createdTime", drop.CreatedTime);
            command.Parameters.AddWithValue("@type", drop.Type);
            command.Parameters.AddWithValue("@location", drop.Location);
            command.Parameters.AddWithValue("@expiryTime", drop.ExpiryTime);
            command.Parameters.AddWithValue("@orderDay", drop.OrderDay);
            command.Parameters.AddWithValue("@isCollected", drop.IsCollected ? 1 : 0);
            command.Parameters.AddWithValue("@initialItemCount", drop.InitialItemCount);
            command.Parameters.AddWithValue("@orderedTierId", drop.OrderedTierId);
            command.Parameters.AddWithValue("@orderedTierName", drop.OrderedTierName);
            command.Parameters.AddWithValue("@pricePaid", drop.PricePaid);
            command.ExecuteNonQuery();

            using var idCommand = connection.CreateCommand();
            idCommand.Transaction = transaction;
            idCommand.CommandText = "SELECT last_insert_rowid();";
            object? result = idCommand.ExecuteScalar();
            return Convert.ToInt64(result ?? 0L);
        }

        private static void InsertDropItems(
            SqliteConnection connection,
            SqliteTransaction transaction,
            long dropId,
            IEnumerable<string> items)
        {
            int ordinal = 0;
            foreach (string itemEntry in items)
            {
                (string itemId, int amount) = ParseItemEntry(itemEntry);
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    continue;
                }

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO drop_items (drop_id, ordinal, item_id, amount)
VALUES (@dropId, @ordinal, @itemId, @amount);";
                command.Parameters.AddWithValue("@dropId", dropId);
                command.Parameters.AddWithValue("@ordinal", ordinal++);
                command.Parameters.AddWithValue("@itemId", itemId);
                command.Parameters.AddWithValue("@amount", amount);
                command.ExecuteNonQuery();
            }
        }

        private static void ReplaceOrders(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyDictionary<int, int> mrStacksOrdersToday)
        {
            ExecuteDelete(connection, transaction, "DELETE FROM mr_stacks_daily_orders;");

            foreach ((int day, int orderCount) in mrStacksOrdersToday.OrderBy(kvp => kvp.Key))
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO mr_stacks_daily_orders (day, order_count)
VALUES (@day, @orderCount);";
                command.Parameters.AddWithValue("@day", day);
                command.Parameters.AddWithValue("@orderCount", orderCount);
                command.ExecuteNonQuery();
            }
        }

        private static void ReplaceConversationMessages(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyList<MrStacksMessaging.MessageRecord> messages)
        {
            ExecuteDelete(connection, transaction, "DELETE FROM conversation_messages;");

            foreach (MrStacksMessaging.MessageRecord message in messages)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO conversation_messages (message_id, text, timestamp, game_day, game_time, is_from_player)
VALUES (@messageId, @text, @timestamp, @gameDay, @gameTime, @isFromPlayer);";
                command.Parameters.AddWithValue("@messageId", message.MessageId);
                command.Parameters.AddWithValue("@text", message.Text);
                command.Parameters.AddWithValue("@timestamp", message.Timestamp);
                command.Parameters.AddWithValue("@gameDay", message.GameDay);
                command.Parameters.AddWithValue("@gameTime", message.GameTime);
                command.Parameters.AddWithValue("@isFromPlayer", message.IsFromPlayer ? 1 : 0);
                command.ExecuteNonQuery();
            }
        }

        private static void ExecuteDelete(SqliteConnection connection, SqliteTransaction transaction, string sql)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private static int CountRows(SqliteConnection connection, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {tableName};";
            object? result = command.ExecuteScalar();
            return Convert.ToInt32(result ?? 0);
        }

        private static (string itemId, int amount) ParseItemEntry(string itemEntry)
        {
            if (string.IsNullOrWhiteSpace(itemEntry))
            {
                return (string.Empty, 1);
            }

            int separatorIndex = itemEntry.IndexOf(':');
            if (separatorIndex > 0 &&
                separatorIndex < itemEntry.Length - 1 &&
                int.TryParse(itemEntry[(separatorIndex + 1)..], out int amount))
            {
                return (itemEntry[..separatorIndex], Math.Max(amount, 1));
            }

            return (itemEntry, 1);
        }

        private static string FormatItemEntry(string itemId, int amount)
        {
            return amount <= 1 ? itemId : $"{itemId}:{amount}";
        }

        private static string GetString(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

    }
}
