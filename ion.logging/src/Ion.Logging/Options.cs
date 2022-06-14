using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Ion.Logging
{
    public class Options
    {
        public const string SectionKey = "Ion:Logging";

        public bool EnableSelfLog { get; set; } = false;

        [Required]
        public LogLevel Level { get; set; } = LogLevel.Information;
    }
}