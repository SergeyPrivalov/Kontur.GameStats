using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Kontur.GameStats.Server
{
    /// <summary>
    /// Вычисляет и агрегирует статистику по игровым серверам, игрокам и матчам.
    /// </summary>
    public class GameStatistic
    {
        /// <summary>
        /// Вычисляет агрегированную статистику сервера на основе массива игровых матчей. Использует GetDivision для вычисления средних значений и GetTopElements для определения топ-5 режимов и карт.
        /// </summary>
        /// <param name="games">Массив игровых матчей для анализа.</param>
        /// <returns>Объект ServerStats с агрегированной статистикой сервера.</returns>
        public ServerStats GetServerStatistic(GameServer[] games)
        {
            var groupByDate = games.GroupBy(x => x.DateAndTime.Date).ToArray();
            return new ServerStats
            {
                TotalMatchesPlayed = games.Length,
                MaximumMatchesPerDay = groupByDate.Max(x => x.Count()),
                AverageMatchesPerDay = GetDivision(games.Length, groupByDate.Length),
                MaximumPopulation = games.Max(x => x.Scoreboard.Length),
                AveragePopulation = GetDivision(games.Sum(x => x.Scoreboard.Length), games.Length),
                Top5GameModes = GetTopElements(5, games.GroupBy(x => x.GameMode)),
                Top5Maps = GetTopElements(5, games.GroupBy(x => x.Map))
            };
        }

        /// <summary>
        /// Выполняет деление двух чисел и округляет результат до 6 знаков после запятой.
        /// </summary>
        /// <param name="x">Делимое.</param>
        /// <param name="y">Делитель.</param>
        /// <returns>Результат деления, округленный до 6 знаков после запятой.</returns>
        private double GetDivision(double x, double y)
        {
            return Math.Round(x / y, 6);
        }

        /// <summary>
        /// Получает топ элементов из группировки по количеству вхождений.
        /// </summary>
        /// <param name="countOfElements">Количество элементов для возврата.</param>
        /// <param name="game">Группировка игровых серверов по ключу.</param>
        /// <returns>Массив ключей топ элементов, отсортированных по убыванию количества вхождений.</returns>
        private string[] GetTopElements(int countOfElements, IEnumerable<IGrouping<string, GameServer>> game)
        {
            return game
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .Take(countOfElements)
                .ToArray();
        }

        /// <summary>
        /// Вычисляет агрегированную статистику игрока на основе массива игровых матчей. Использует GetTopElements для определения любимого сервера и режима игры, GetAverageScoreboardPercent для вычисления среднего процента позиции, GetDivision для вычисления средних значений, GetPlayerStats для извлечения статистики игрока и GetKillToDeathRatio для вычисления соотношения убийств к смертям.
        /// </summary>
        /// <param name="name">Имя игрока для анализа.</param>
        /// <param name="games">Массив игровых матчей, в которых участвовал игрок.</param>
        /// <returns>Объект PlayerStats с агрегированной статистикой игрока.</returns>
        public PlayerStats GetPlayerStatistic(string name, GameServer[] games)
        {
            var groupByEndpoint = games.GroupBy(x => x.Endpoint).ToArray();
            var groupByDate = games.GroupBy(x => x.DateAndTime.Date).ToArray();
            return new PlayerStats
            {
                TotalMatchesPlayed = games.Length,
                TotalMatchesWon =
                    games.Count(x => x.Scoreboard[0].Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)),
                FavoriteServer = GetTopElements(1, groupByEndpoint)[0],
                UniqueServers = groupByEndpoint.Count(),
                FavoriteGameMode = GetTopElements(1, games.GroupBy(x => x.GameMode))[0],
                AverageScoreboardPercent = GetAverageScoreboardPercent(games, name),
                MaximumMatchesPerDay = groupByDate.Max(x => x.Count()),
                AverageMatchesPerDay = GetDivision(games.Length, groupByDate.Length),
                LastMatchPlayed = games.Max(x => x.DateAndTime).ToString("yyyy-MM-dTHH:mm:ssZ"),
                KillToDeathRatio = GetKillToDeathRatio(GetPlayerStats(games, name))
            };
        }

        /// <summary>
        /// Извлекает статистику игрока по имени из всех указанных игровых серверов.
        /// </summary>
        /// <param name="gameServers">Коллекция игровых серверов для поиска.</param>
        /// <param name="name">Имя игрока для поиска (без учета регистра).</param>
        /// <returns>Массив объектов Player, представляющих статистику игрока во всех найденных матчах.</returns>
        private Player[] GetPlayerStats(IEnumerable<GameServer> gameServers, string name)
        {
            return gameServers
                .SelectMany(x => x.Scoreboard
                    .Where(y => y.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                .ToArray();
        }

        /// <summary>
        /// Вычисляет соотношение убийств к смертям на основе статистики игрока. Использует GetDivision для округления результата до 6 знаков после запятой.
        /// </summary>
        /// <param name="stats">Массив статистики игрока из различных матчей.</param>
        /// <returns>Соотношение убийств к смертям, округленное до 6 знаков после запятой.</returns>
        private double GetKillToDeathRatio(Player[] stats)
        {
            return GetDivision(stats.Sum(x => x.Kills), stats.Sum(x => x.Deaths));
        }

        /// <summary>
        /// Вычисляет средний процент позиции игрока в таблице результатов по всем матчам.
        /// </summary>
        /// <param name="gameServers">Список игровых серверов для анализа.</param>
        /// <param name="name">Имя игрока для поиска (без учета регистра).</param>
        /// <returns>Средний процент позиции в таблице результатов, округленный до 6 знаков после запятой.</returns>
        private double GetAverageScoreboardPercent(IReadOnlyList<GameServer> gameServers, string name)
        {
            var averageScoreboard = new double[gameServers.Count];
            for (var j = 0; j < gameServers.Count; ++j)
            {
                var scoreboardLength = gameServers[j].Scoreboard.Length;
                for (var i = 0; i < scoreboardLength; ++i)
                    if (gameServers[j].Scoreboard[i].Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                        averageScoreboard[j] = (double) (scoreboardLength - (i + 1))
                                               / (scoreboardLength - 1) * 100;
            }
            return Math.Round(averageScoreboard.Average(), 6);
        }

        /// <summary>
        /// Получает список недавних матчей, отсортированных по дате в порядке убывания.
        /// </summary>
        /// <param name="countOfMatches">Количество матчей для возврата.</param>
        /// <param name="gameServers">Коллекция игровых серверов для анализа.</param>
        /// <returns>Массив объектов RecentMatch, представляющих недавние матчи, отсортированные по дате.</returns>
        public RecentMatch[] GetRecentMatches(int countOfMatches, IEnumerable<GameServer> gameServers)
        {
            return gameServers
                .OrderByDescending(x => x.DateAndTime)
                .Take(countOfMatches)
                .Select(x =>
                    new RecentMatch
                    {
                        Server = x.Endpoint,
                        Timestamp = x.DateAndTime.ToString("yyyy-MM-dTHH:mm:ssZ"),
                        Result = x
                    })
                .ToArray();
        }

        /// <summary>
        /// Получает список лучших игроков на основе соотношения убийств к смертям. Использует GetPlayerStats для извлечения статистики каждого игрока и GetKillToDeathRatio для вычисления соотношения. Учитываются только игроки, сыгравшие не менее 10 матчей.
        /// </summary>
        /// <param name="countOfPlayers">Количество игроков для возврата.</param>
        /// <param name="gameServers">Коллекция игровых серверов для анализа.</param>
        /// <returns>Массив объектов BestPlayer, отсортированных по убыванию соотношения убийств к смертям.</returns>
        public BestPlayer[] GetBestPlayers(int countOfPlayers, BlockingCollection<GameServer> gameServers)
        {
            var playersNames = gameServers
                .SelectMany(x => x.Scoreboard)
                .GroupBy(x => x.Name)
                .Where(x => x.Count() >= 10)
                .Select(x => x.Key);
            var listOfPlayers = new List<BestPlayer>();
            foreach (var name in playersNames)
            {
                var stats = GetPlayerStats(gameServers.ToArray(), name);
                if (stats.Sum(x => x.Deaths) != 0)
                    listOfPlayers.Add(new BestPlayer
                    {
                        Name = name,
                        KillToDeathRatio = GetKillToDeathRatio(stats)
                    });
            }
            return listOfPlayers
                .OrderByDescending(x => x.KillToDeathRatio)
                .Take(countOfPlayers)
                .ToArray();
        }

        /// <summary>
        /// Получает список популярных серверов на основе среднего количества матчей в день. Использует GetAverageMatchPerDay для вычисления среднего количества матчей в день для каждого сервера.
        /// </summary>
        /// <param name="countOfServers">Количество серверов для возврата.</param>
        /// <param name="advertiseServers">Словарь зарегистрированных серверов.</param>
        /// <param name="gameServers">Коллекция игровых серверов для анализа.</param>
        /// <returns>Массив объектов PopularServer, отсортированных по убыванию среднего количества матчей в день.</returns>
        public PopularServer[] GetPopularServers(
            int countOfServers,
            ConcurrentDictionary<string, AdvertiseQueryServer> advertiseServers,
            IEnumerable<GameServer> gameServers)
        {
            return advertiseServers
                .Select(x => new PopularServer
                {
                    Enpoint = x.Key,
                    Name = x.Value.Info.Name,
                    AverageMatchPerDay = GetAverageMatchPerDay(x.Key, gameServers.ToArray())
                })
                .OrderByDescending(x => x.AverageMatchPerDay)
                .Take(countOfServers)
                .ToArray();
        }

        /// <summary>
        /// Вычисляет среднее количество матчей в день для указанного сервера. Использует GetDivision для вычисления среднего значения с округлением до 6 знаков после запятой.
        /// </summary>
        /// <param name="enpoint">Endpoint сервера для анализа.</param>
        /// <param name="gameServers">Массив игровых серверов для анализа.</param>
        /// <returns>Среднее количество матчей в день, округленное до 6 знаков после запятой.</returns>
        public double GetAverageMatchPerDay(string enpoint, GameServer[] gameServers)
        {
            return GetDivision(
                gameServers
                    .GroupBy(x => x.Endpoint)
                    .Count(x => x.Key == enpoint),
                gameServers
                    .Where(x => x.Endpoint == enpoint)
                    .GroupBy(x => x.DateAndTime.Date).Count());
        }
    }
}