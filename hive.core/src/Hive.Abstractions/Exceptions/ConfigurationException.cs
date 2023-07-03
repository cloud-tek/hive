namespace Hive.Exceptions;

public class ConfigurationException : Exception
{
    public string? Key { get; protected init; } = default!;

    public ConfigurationException(string? message)
        : base(message)
    {
    }

    public ConfigurationException(string? message, string? key)
        : base(message)
    {
        Key = key;
    }

    protected ConfigurationException()
    {}
}
