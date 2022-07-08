using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Hive.Logging
{
    public class Options
    {
        public const string SectionKey = "Hive:Logging";

        public bool EnableSelfLog { get; set; } = false;

        [Required]
        public LogLevel Level { get; set; } = LogLevel.Information;
    }
}