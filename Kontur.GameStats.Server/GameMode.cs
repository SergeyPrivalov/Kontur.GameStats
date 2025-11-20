using System.ComponentModel.DataAnnotations.Schema;

namespace Kontur.GameStats.Server
{
    /// <summary>
    /// Представляет игровой режим, поддерживаемый сервером.
    /// </summary>
    [Table("GameModes")]
    internal class GameMode
    {
        [Column("endpoint")]
        public string Endpoint { get; set; }

        [Column("mode")]
        public string Mode { get; set; }
    }
}