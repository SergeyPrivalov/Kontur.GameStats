using System.Collections.Concurrent;

namespace Kontur.GameStats.Server
{
    public interface IServerDataBase
    {
        void AddAdvertServer(AdvertiseQueryServer advertiseQueryServer);
        void AddGameServer(GameServer gameServer);
        void UpdateAdvertServer(AdvertiseQueryServer advertiseQueryServer);
        ConcurrentDictionary<string, AdvertiseQueryServer> ReadAdvertServers();
        BlockingCollection<GameServer> ReadGameServers();
    }
}
