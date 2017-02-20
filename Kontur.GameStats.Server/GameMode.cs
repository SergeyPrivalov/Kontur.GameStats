﻿using System.ComponentModel.DataAnnotations.Schema;

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
