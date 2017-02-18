using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;

namespace Kontur.GameStats.Server
{
    public class ServerDataBase
    {
        private readonly string baseName = "D:\\StatisticServer.db";
        private readonly SQLiteConnection connection;

        public ServerDataBase()
        {
            SQLiteConnection.CreateFile(baseName);
            //var factory = (SQLiteFactory) DbProviderFactories.GetFactory("System.Data.SQLite");
            using (connection = new SQLiteConnection($"Data Source={baseName};"))//(SQLiteConnection) factory.CreateConnection())
            {
                //connection.ConnectionString = "Data Source = " + baseName;
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
            const string gameModesTable = @"CREATE TABLE [GameModes] (" +
                                          "[id] integer PRIMARY KEY AUTOINCREMENT NOT NULL," +
                                          "[endpoint] char(45) NOT NULL," +
                                          "[mode] char(5) NOT NULL);";
            const string gameServersTable = @"CREATE TABLE [GameServers] (" +
                                            "[id] integer PRIMARY KEY AUTOINCREMENT NOT NULL," +
                                            "[endpoint] char(45) NOT NULL," +
                                            "[date] char(20) NOT NULL," +
                                            "[mode] char(5) NOT NULL," +
                                            "[map] char(60) NOT NULL" +
                                            "[fragLimit] int NOT NULL," +
                                            "[timeLimit] int NOT NULL," +
                                            "[timeElapsed] REAL NOT NULL);";
            const string plaersTable = @"CREATE TABLE [Players] (" +
                                       "[id] integer PRIMARY KEY AUTOINCREMENT NOT NULL," +
                                       "[endpoint] char(45) NOT NULL," +
                                       "[date] char(20) NOT NULL," +
                                       "[name] char(60) NOT NULL" +
                                       "[frags] int NOT NULL" +
                                       "[kills] int NOT NULL" +
                                       "[deaths] int NOT NULL);";
            ExecuteCommand(advertTable + gameModesTable + gameServersTable + plaersTable);
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
            var command = "INSERT INTO 'AdvertiseServers' ('endpoint','name') VALUES " +
                          $"({advertiseQueryServer.Endpoint},{advertiseQueryServer.Info.Name});";
            foreach (var gameMode in advertiseQueryServer.Info.GameModes)
                command += "INSERT INTO 'GameModes' ('endpoint','mode') VALUES " +
                           $"({advertiseQueryServer.Endpoint},{gameMode});";
            ExecuteCommand(command);
        }

        public void AddGameServer(GameServer gameServer)
        {
            var command = "INSERT INTO 'GameServers' " +
                          "('endpoint','date','map','fragLimit','timeLimit','timeElapsed') VALUES " +
                          $"({gameServer.Endpoint},{gameServer.DateAndTime},{gameServer.Map}," +
                          $"{gameServer.FragLimit},{gameServer.TimeLimit},{gameServer.TimeElapsed});";
            foreach (var player in gameServer.Scoreboard)
                command += "INSERT INTO 'Players' " +
                           "('endpoint','date','name','frags','kills','deaths') VALUES " +
                           $"({gameServer.Endpoint},{gameServer.DateAndTime},{player.Name}," +
                           $"{player.Frags},{player.Kills},{player.Deaths});";
            ExecuteCommand(command);
        }

        public void GetAllData()
        {
            var data = new DataSet();
            data.Reset();
            var ad = new SQLiteDataAdapter();
            ad.Fill(data);
            ReadAdvertServers(data.Tables[0], data.Tables[1]);
            ReadGameServers(data.Tables[2], data.Tables[3]);
        }

        private void ReadAdvertServers(DataTable advertTable, DataTable modesTable)
        {
            var dictionary = new Dictionary<string, List<string>>();
            foreach (DataRow row in modesTable.Rows)
            {
                var endpoint = row[1].ToString();
                if (!dictionary.ContainsKey(endpoint))
                    dictionary.Add(endpoint, new List<string>());
                dictionary[endpoint].Add(row[2].ToString());
            }
            foreach (DataRow row in advertTable.Rows)
            {
                var endpoint = row[1].ToString();
                QueryProcessor.AdvertiseServers.Add(new AdvertiseQueryServer(endpoint,
                    new Information { Name = row[2].ToString(), GameModes = dictionary[endpoint].ToArray() }));
            }
        }

        private void ReadGameServers(DataTable serversTable, DataTable playersTable)
        {
            var playerDictionary = new Dictionary<string, Dictionary<DateTime, List<Player>>>();
            foreach (DataRow row in playersTable.Rows)
            {
                var endpoint = row[1].ToString();
                var date = DateTime.Parse(row[2].ToString());
                if (!playerDictionary.ContainsKey(endpoint))
                    playerDictionary.Add(endpoint, new Dictionary<DateTime, List<Player>>());
                if (!playerDictionary[endpoint].ContainsKey(date))
                    playerDictionary[endpoint].Add(date, new List<Player>());
                playerDictionary[endpoint][date].Add(new Player
                {
                    Name = row[3].ToString(),
                    Frags = int.Parse(row[4].ToString()),
                    Kills = int.Parse(row[5].ToString()),
                    Deaths = int.Parse(row[6].ToString())
                });
            }
            foreach (DataRow row in serversTable.Rows)
            {
                var endpoint = row[1].ToString();
                var date = DateTime.Parse(row[2].ToString());
                QueryProcessor.GameServers.Add(new GameServer
                {
                    Endpoint = endpoint,
                    DateAndTime = date,
                    GameMode = row[3].ToString(),
                    Map = row[4].ToString(),
                    FragLimit = int.Parse(row[5].ToString()),
                    TimeLimit = int.Parse(row[6].ToString()),
                    TimeElapsed = double.Parse(row[7].ToString()),
                    Scoreboard = playerDictionary[endpoint][date].ToArray()
                });
            }
        }
    }
}