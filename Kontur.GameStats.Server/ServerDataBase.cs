﻿using System.Linq;
using SQLite;

namespace Kontur.GameStats.Server
{
    public class ServerDataBase
    {
        private readonly string baseName = "StatisticServer.db";

        public ServerDataBase()
        {
            using (var connection = new SQLiteConnection($"{baseName}", true))
            {
                connection.CreateTable<AdvertiseQueryServer>();
                connection.CreateTable<GameMode>();
                connection.CreateTable<GameServer>();
                connection.CreateTable<Player>();
            }
        }

        public void AddAdvertServer(AdvertiseQueryServer advertiseQueryServer)
        {
            var connection = new SQLiteConnection($"{baseName}", true);
            connection.Insert(advertiseQueryServer);
            foreach (var gameMode in advertiseQueryServer.Info.GameModes)
                connection.Insert(new GameMode
                {
                    Endpoint = advertiseQueryServer.Endpoint,
                    Mode = gameMode
                });
        }

        public void AddGameServer(GameServer gameServer)
        {
            var connection = new SQLiteConnection($"{baseName}", true);
            connection.Insert(gameServer);
            var players = gameServer.Scoreboard;
            for (var i = 0; i < players.Length; ++i)
                connection.Insert(new Player
                {
                    Date = gameServer.DateAndTime,
                    Endpoint = gameServer.Endpoint,
                    Place = i,
                    Name = players[i].Name,
                    Deaths = players[i].Deaths,
                    Frags = players[i].Frags,
                    Kills = players[i].Kills
                });
        }

        public void GetAllData()
        {
            var connection = new SQLiteConnection($"{baseName}", true);
            ReadAdvertServers(connection);
            ReadGameServers(connection);
        }

        private void ReadAdvertServers(SQLiteConnection connection)
        {
            var advertServers = connection.Table<AdvertiseQueryServer>();
            var gameModes = connection.Table<GameMode>();
            foreach (var advertServer in advertServers)
            {
                var adServer = advertServer;
                adServer.Info.Name = advertServer.Name;
                adServer.Info.GameModes = gameModes
                    .Where(x => x.Endpoint == advertServer.Endpoint)
                    .Select(x => x.Mode).ToArray();
                QueryProcessor.AdvertiseServers.Add(adServer);
            }
        }

        private void ReadGameServers(SQLiteConnection connection)
        {
            var gameServers = connection.Table<GameServer>();
            var players = connection.Table<Player>();
            foreach (var gameServer in gameServers)
            {
                var scoreboard = players
                    .Where(x => x.Endpoint == gameServer.Endpoint)
                    .Where(x => x.Date == gameServer.DateAndTime)
                    .OrderBy(x => x.Place)
                    .ToArray();
                QueryProcessor.GameServers.Add(new GameServer
                {
                    DateAndTime = gameServer.DateAndTime,
                    Endpoint = gameServer.Endpoint,
                    Map = gameServer.Map,
                    GameMode = gameServer.GameMode,
                    FragLimit = gameServer.FragLimit,
                    TimeElapsed = gameServer.TimeElapsed,
                    TimeLimit = gameServer.TimeLimit,
                    Scoreboard = scoreboard
                });
            }
        }
    }
}