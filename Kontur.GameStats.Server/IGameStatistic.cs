using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Kontur.GameStats.Server
{
    public interface IGameStatistic
    {
        ServerStats GetServerStatistic(GameServer[] games);
        PlayerStats GetPlayerStatistic(string name, GameServer[] games);
        RecentMatch[] GetRecentMatches(int countOfMatches, IEnumerable<GameServer> gameServers);
        BestPlayer[] GetBestPlayers(int countOfPlayers, BlockingCollection<GameServer> gameServers);

        PopularServer[] GetPopularServers(int countOfServers,
            ConcurrentDictionary<string, AdvertiseQueryServer> advertiseServers,
            IEnumerable<GameServer> gameServers);

        double GetAverageMatchPerDay(string enpoint, GameServer[] gameServers);
    }
}