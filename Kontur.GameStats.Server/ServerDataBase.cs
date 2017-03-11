﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SQLite;

namespace Kontur.GameStats.Server
{
    public class ServerDataBase
    {
        private readonly string baseName = "StatisticServer.db";

        public ServerDataBase()
        {
            using (var connection = new SQLiteConnection($"{baseName}", true))//тут true по дефолту. можно не писать явно
            {
                connection.CreateTable<AdvertiseQueryServer>();
                connection.CreateTable<GameMode>();
                connection.CreateTable<GameServer>();
                connection.CreateTable<Player>();
            }
        }

        public void AddAdvertServer(AdvertiseQueryServer advertiseQueryServer)
        {
            using (var connection = new SQLiteConnection($"{baseName}", true))//тут true по дефолту. можно не писать явно
            {
                connection.Insert(advertiseQueryServer);
                foreach (var gameMode in advertiseQueryServer.Info.GameModes)
                    connection.Insert(new GameMode
                    {
                        Endpoint = advertiseQueryServer.Endpoint,
                        Mode = gameMode
                    });
            }
        }

        public void AddGameServer(GameServer gameServer)
        {
            using (var connection = new SQLiteConnection($"{baseName}", true))//тут true по дефолту. можно не писать явно
            {
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
        }

        public void UpdateAdvertServer(AdvertiseQueryServer advertiseQueryServer)
        {
            using (var connection = new SQLiteConnection($"{baseName}", true))//тут true по дефолту. можно не писать явно
            {
                connection.CreateCommand(
                    $"DELETE FROM AdvertiseQueryServer WHERE endpoint = {advertiseQueryServer.Endpoint}; " +
                    $"DELETE FROM GameModes WHERE endpoint = {advertiseQueryServer.Endpoint}");
                connection.Commit();
                AddAdvertServer(advertiseQueryServer);
            }
        }

        public ConcurrentDictionary<string, AdvertiseQueryServer> ReadAdvertServers()
        {
            var serversCollection = new ConcurrentDictionary<string, AdvertiseQueryServer>();
            using (var connection = new SQLiteConnection($"{baseName}", true))//тут true по дефолту. можно не писать явно
            {
                var advertServers = connection.Table<AdvertiseQueryServer>();
                var gameModes = connection.Table<GameMode>();
                var modeDictionary = new Dictionary<string, List<string>>();
                foreach (var gameMode in gameModes)
                {
                    if (!modeDictionary.ContainsKey(gameMode.Endpoint))
                        modeDictionary.Add(gameMode.Endpoint, new List<string>());
                    modeDictionary[gameMode.Endpoint].Add(gameMode.Mode);
                }
                foreach (var advertServer in advertServers)
                {
                    var newServer = new AdvertiseQueryServer
                    {
                        Endpoint = advertServer.Endpoint,
                        Name = advertServer.Name,
                        Info = new Information
                        {
                            Endpoint = advertServer.Endpoint,
                            Name = advertServer.Name,
                            GameModes = modeDictionary[advertServer.Endpoint].ToArray()
                        }
                    };
                    serversCollection.AddOrUpdate(advertServer.Endpoint, newServer, (s, server) => newServer);
                }
            }
            return serversCollection;
        }

        public BlockingCollection<GameServer> ReadGameServers()
        {
            using (var connection = new SQLiteConnection($"{baseName}", true))
            {
                var gamesCollection = new BlockingCollection<GameServer>();
                var gameServers = connection.Table<GameServer>();
                var players = connection.Table<Player>();
                foreach (var gameServer in gameServers)
                {
                    var scoreboard = players
                        //кажется эти 2 Where можно объединить в один. 
                        //Опять же каждый вызов этого метода под капотом создает огромную кучу дополниетльных объектов в куче
                        //а здесь это не имеет смысл
                        .Where(x => x.Endpoint == gameServer.Endpoint)
                        .Where(x => x.Date == gameServer.DateAndTime)
                        .OrderBy(x => x.Place)
                        .ToArray();
                    gamesCollection.Add(new GameServer
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
                return gamesCollection;
            }
        }
    }
}