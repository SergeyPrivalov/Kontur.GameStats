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
        private readonly string baseName = "StatisticServer.db";
        private SQLiteConnection connection;

        public ServerDataBase()
        {
            //SQLiteConnection.CreateFile(baseName);
            using (connection = new SQLiteConnection($"Data Source={baseName};"))
            {
                if (!File.Exists(baseName)) CreateDb();
            }
        }

        private void CreateDb()
        {
            SQLiteConnection.CreateFile(baseName);
            connection.Open();
            const string advertTable = @"CREATE TABLE AdvertiseServers(" +
                                       "id integer PRIMARY KEY AUTOINCREMENT NOT NULL," +
                                       "endpoint char(45) NOT NULL," +
                                       "name char(60) NOT NULL);";
            const string gameModesTable = @"CREATE TABLE GameModes (" +
                                          "id integer PRIMARY KEY AUTOINCREMENT NOT NULL," +
                                          "endpoint char(45) NOT NULL," +
                                          "mode char(5) NOT NULL);";
            const string gameServersTable = @"CREATE TABLE GameServers (" +
                                            "id integer PRIMARY KEY AUTOINCREMENT NOT NULL," +
                                            "endpoint char(45) NOT NULL," +
                                            "date char(20) NOT NULL," +
                                            "mode char(5) NOT NULL," +
                                            "map char(60) NOT NULL" +
                                            "fragLimit int NOT NULL," +
                                            "timeLimit int NOT NULL," +
                                            "timeElapsed REAL NOT NULL);";
            const string plaersTable = @"CREATE TABLE Players (" +
                                       "id integer PRIMARY KEY AUTOINCREMENT NOT NULL," +
                                       "endpoint char(45) NOT NULL," +
                                       "date char(20) NOT NULL," +
                                       "name char(60) NOT NULL" +
                                       "frags int NOT NULL" +
                                       "kills int NOT NULL" +
                                       "deaths int NOT NULL);";
            ExecuteCommand(advertTable + gameModesTable + gameServersTable + plaersTable);
            connection.Close();
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
            connection = new SQLiteConnection($"Data Source={baseName};");
            connection.Open();
            var advertCommand = new SQLiteCommand("SELECT * FROM AdvertiseServers",connection);
            var modeCommand = new SQLiteCommand("SELECT * FROM GameModes", connection);
            var serverCommand = new SQLiteCommand("SELECT * FROM GameServers", connection);
            var scoreboardCommand = new SQLiteCommand("SELECT * FROM Players", connection);
            try
            {
                ReadAdvertServers(advertCommand.ExecuteReader(), modeCommand.ExecuteReader());
                ReadGameServers(serverCommand.ExecuteReader(), scoreboardCommand.ExecuteReader());
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

        private void ReadAdvertServers(SQLiteDataReader advertTable, SQLiteDataReader modesTable)
        {
            var dictionary = new Dictionary<string, List<string>>();
            while (modesTable.Read())
            {
                var endpoint = modesTable["endpoint"].ToString();
                if (!dictionary.ContainsKey(endpoint))
                    dictionary.Add(endpoint, new List<string>());
                dictionary[endpoint].Add(modesTable["name"].ToString());
            }
            while (advertTable.Read())
            {
                var endpoint = advertTable["endpoint"].ToString();
                QueryProcessor.AdvertiseServers.Add(new AdvertiseQueryServer(endpoint,
                    new Information
                    {
                        Name = advertTable["name"].ToString(),
                        GameModes = dictionary[endpoint].ToArray()
                    }));
            }
        }

        private void ReadGameServers(SQLiteDataReader serversTable, SQLiteDataReader playersTable)
        {
            var playerDictionary = new Dictionary<string, Dictionary<DateTime, List<Player>>>();
            while (playersTable.Read())
            {
                var endpoint = playersTable["endpoint"].ToString();
                var date = DateTime.Parse(playersTable["date"].ToString());
                if (!playerDictionary.ContainsKey(endpoint))
                    playerDictionary.Add(endpoint, new Dictionary<DateTime, List<Player>>());
                if (!playerDictionary[endpoint].ContainsKey(date))
                    playerDictionary[endpoint].Add(date, new List<Player>());
                playerDictionary[endpoint][date].Add(new Player
                {
                    Name = playersTable["name"].ToString(),
                    Frags = int.Parse(playersTable["frags"].ToString()),
                    Kills = int.Parse(playersTable["kills"].ToString()),
                    Deaths = int.Parse(playersTable["deaths"].ToString())
                });
            }
            while (serversTable.Read())
            {
                var endpoint = serversTable["endpoint"].ToString();
                var date = DateTime.Parse(serversTable["date"].ToString());
                QueryProcessor.GameServers.Add(new GameServer
                {
                    Endpoint = endpoint,
                    DateAndTime = date,
                    GameMode = serversTable["mode"].ToString(),
                    Map = serversTable["map"].ToString(),
                    FragLimit = int.Parse(serversTable["fragLimit"].ToString()),
                    TimeLimit = int.Parse(serversTable["timeLimit"].ToString()),
                    TimeElapsed = double.Parse(serversTable["timeElapsed"].ToString()),
                    Scoreboard = playerDictionary[endpoint][date].ToArray()
                });
            }
        }
    }
}