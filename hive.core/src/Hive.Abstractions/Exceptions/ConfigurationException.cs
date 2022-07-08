namespace Hive.Exceptions;

public class ConfigurationException : Exception
{
    public string Key { get; private set; }

    public ConfigurationException(string message)
        : base(message)
    {
    }

    public ConfigurationException(string message, string key)
        : base(message)
    {
        Key = key;
    }

    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ConfigurationException(string message, string key, Exception innerException)
        : base(message, innerException)
    {
        Key = key;
    }
}
