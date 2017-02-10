using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.GameStats.Server
{
    public class GameStatistic
    {
        public static ServerStats GetStatistic(string endpoint)
        {
            var games = QueryProcessor.GameServers.Where(x => x.Endpoint == endpoint);
            var totalMatchesPlayed = games.Count();
            var maxMatchesPerDay = games
                .GroupBy(x => x.Date)
                .Max(x => x.Count());
            var averageMatchesPerDay = (double)totalMatchesPlayed / games.GroupBy(x =>x.Date).Count();
            var maxPopulation = games.Max(x => x.Scoreboard.Length);
            var averagePopulation = (double) games.Sum(x => x.Scoreboard.Length) / totalMatchesPlayed;
            var top5Modes = games.GroupBy(x => x.GameMode)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key).Take(5).ToArray();
            var top5Maps = games.GroupBy(x => x.Map)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key).Take(5).ToArray();
            return new ServerStats(totalMatchesPlayed, maxMatchesPerDay, averageMatchesPerDay,
                maxPopulation, averagePopulation, top5Modes, top5Maps);
        }
    }
}
