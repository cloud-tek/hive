namespace Ion.Logging.LogzIo;

public class Options
{
    public const string SectionKey = "Ion:Logging:LogzIo";

    /// <summary>
    /// LogzIo Token
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// LogzIo Region : us | eu
    /// </summary>
    public string Region { get; set; }
}
