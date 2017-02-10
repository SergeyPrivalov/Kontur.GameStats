using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.GameStats.Server
{
    class CollectedServStats
    {
        public int TotalMatches { get; set; } = 0;
        public int MaxPopulation { get; set; } = 0;
        public int TotalPopulation { get; set; } = 0;
        public Dictionary<string,int> Top5Modes { get; set; } 
            = new Dictionary<string, int>();
        public Dictionary<string,int> Top5Maps { get; set; } 
            = new Dictionary<string, int>();

        public void AddStats(GameServer gameServer)
        {
            TotalMatches++;
            var countOfPlayer = gameServer.Scoreboard.Length;
            TotalPopulation += countOfPlayer;
            if (MaxPopulation < countOfPlayer)
                MaxPopulation = countOfPlayer;
            AddMap(gameServer.Map);
            AddMode(gameServer.GameMode);
        }

        private void AddMode(string mode)
        {
            if (!Top5Modes.ContainsKey(mode))
                Top5Modes.Add(mode,0);
            Top5Modes[mode]++;
        }

        private void AddMap(string map)
        {
            if(!Top5Maps.ContainsKey(map))
                Top5Maps.Add(map,0);
            Top5Maps[map]++;
        }
    }
}
