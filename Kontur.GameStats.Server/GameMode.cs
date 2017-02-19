using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.GameStats.Server
{
    [Table("GameModes")]
    class GameMode
    {
        [Column("endpoint")]
        public string Endpoint { get; set; }

        [Column("mode")]
        public string Mode { get; set; }
    }
}
