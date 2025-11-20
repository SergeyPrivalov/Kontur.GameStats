using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SQLite;

namespace Kontur.GameStats.Server
{
    /// <summary>
    /// Отвечает за хранение и извлечение данных о серверах и матчах из базы данных.
    /// </summary>
    public class ServerDataBase
    {
        private readonly string baseName = "StatisticServer.db";

        /// <summary>
        /// Инициализирует новый экземпляр класса ServerDataBase и создает необходимые таблицы в базе данных.
        /// </summary>
        public ServerDataBase()
        {
            using (var connection = new SQLiteConnection($"{baseName}"))
            {
                connection.CreateTable<AdvertiseQueryServer>();
                connection.CreateTable<GameMode>();
                connection.CreateTable<GameServer>();
                connection.CreateTable<Player>();
            }
        }

        /// <summary>
        /// Добавляет информацию о рекламируемом сервере в базу данных, включая все поддерживаемые игровые режимы.
        /// </summary>
        /// <param name="advertiseQueryServer">Объект AdvertiseQueryServer с информацией о сервере для добавления.</param>
        public void AddAdvertServer(AdvertiseQueryServer advertiseQueryServer)
        {
            using (var connection = new SQLiteConnection($"{baseName}"))
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

        /// <summary>
        /// Добавляет информацию об игровом сервере (матче) в базу данных, включая статистику всех игроков из таблицы результатов.
        /// </summary>
        /// <param name="gameServer">Объект GameServer с информацией о матче для добавления.</param>
        public void AddGameServer(GameServer gameServer)
        {
            using (var connection = new SQLiteConnection($"{baseName}"))
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

        /// <summary>
        /// Обновляет информацию о рекламируемом сервере в базе данных. Удаляет старую информацию и добавляет новую. Использует AddAdvertServer для добавления обновленной информации.
        /// </summary>
        /// <param name="advertiseQueryServer">Объект AdvertiseQueryServer с обновленной информацией о сервере.</param>
        public void UpdateAdvertServer(AdvertiseQueryServer advertiseQueryServer)
        {
            using (var connection = new SQLiteConnection($"{baseName}"))
            {
                connection.CreateCommand(
                    $"DELETE FROM AdvertiseQueryServer WHERE endpoint = {advertiseQueryServer.Endpoint}; " +
                    $"DELETE FROM GameModes WHERE endpoint = {advertiseQueryServer.Endpoint}");
                connection.Commit();
                AddAdvertServer(advertiseQueryServer);
            }
        }

        /// <summary>
        /// Читает все рекламируемые серверы из базы данных и возвращает их в виде словаря, где ключ - endpoint сервера.
        /// </summary>
        /// <returns>Словарь рекламируемых серверов, где ключ - endpoint, значение - объект AdvertiseQueryServer.</returns>
        public ConcurrentDictionary<string, AdvertiseQueryServer> ReadAdvertServers()
        {
            var serversCollection = new ConcurrentDictionary<string, AdvertiseQueryServer>();
            using (var connection = new SQLiteConnection($"{baseName}"))
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
                            Name = advertServer.Name,
                            GameModes = modeDictionary[advertServer.Endpoint].ToArray()
                        }
                    };
                    serversCollection.AddOrUpdate(advertServer.Endpoint, newServer, (s, server) => newServer);
                }
            }
            return serversCollection;
        }

        /// <summary>
        /// Читает все игровые серверы (матчи) из базы данных и возвращает их в виде коллекции, включая таблицы результатов для каждого матча.
        /// </summary>
        /// <returns>Коллекция игровых серверов с восстановленными таблицами результатов.</returns>
        public BlockingCollection<GameServer> ReadGameServers()
        {
            using (var connection = new SQLiteConnection($"{baseName}"))
            {
                var gamesCollection = new BlockingCollection<GameServer>();
                var gameServers = connection.Table<GameServer>();
                var players = connection.Table<Player>();
                foreach (var gameServer in gameServers)
                {
                    var scoreboard = players
                        .Where(x => x.Endpoint == gameServer.Endpoint && x.Date == gameServer.DateAndTime)
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