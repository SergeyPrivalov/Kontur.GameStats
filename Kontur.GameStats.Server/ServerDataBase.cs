using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;

namespace Kontur.GameStats.Server
{
    public class ServerDataBase
    {
        private string baseName = "StatisticServer.db3";
        private SQLiteConnection connection;

        public ServerDataBase()
        {
            var factory = (SQLiteFactory) DbProviderFactories.GetFactory("System.Data.SQLite");
            using (connection = (SQLiteConnection) factory.CreateConnection())
            {
                connection.ConnectionString = "Data Source = " + baseName;
                if (!File.Exists(baseName)) CreateDb();
                else connection.Open();
            }
        }

        private void CreateDb()
        {
            SQLiteConnection.CreateFile(baseName);
            connection.Open();
            const string advertTable = @"CREATE TABLE [AdvertiseServers] (" +
                                       "[id] integer PRIMARY KEY AUTOINCREMENT NOT NULL," +
                                       "[endpoint] char(45) NOT NULL," +
                                       "[name] char(60) NOT NULL);";
            ExecuteCommand(advertTable);
            const string gameModesTable = @"CREATE TABLE [GameModes] (" +
                                          "[id] integer PRIMARY KEY AUTOINCREMENT NOT NULL," +
                                          "[endpoint] char(45) NOT NULL," +
                                          "[mode] char(5) NOT NULL);";
            ExecuteCommand(gameModesTable);
            const string gameServersTable = @"CREATE TABLE [GameServers] (" +
                                            "[id] integer PRIMARY KEY AUTOINCREMENT NOT NULL," +
                                            "[endpointAndDate] char(65) NOT NULL," +
                                            "[map] char(5) NOT NULL," +
                                            "[fragLimit] int NOT NULL," +
                                            "[timeLimit] int NOT NULL," +
                                            "[timeElapsed] REAL NOT NULL);";
            ExecuteCommand(gameServersTable);
            const string plaersTable = @"CREATE TABLE [Players] (" +
                                       "[id] integer PRIMARY KEY AUTOINCREMENT NOT NULL," +
                                       "[endpointAndDate] char(45) NOT NULL," +
                                       "[place] int NOT NULL" +
                                       "[name] char(60) NOT NULL" +
                                       "[frags] int NOT NULL" +
                                       "[kills] int NOT NULL" +
                                       "[deaths] int NOT NULL);";
            ExecuteCommand(plaersTable);
        }

        private void ExecuteCommand(string commandString)
        {
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = commandString;
                command.CommandType = CommandType.Text;
                command.ExecuteNonQuery();
            }
        }

        public void AddAdvertServer(AdvertiseQueryServer advertiseQueryServer)
        {
            //var command = "INSERT INTO 'AdvertiseServers'"
        }
    }
}
