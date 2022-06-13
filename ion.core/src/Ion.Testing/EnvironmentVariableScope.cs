using System;

namespace Ion.Testing
{
    public class EnvironmentVariableScope : IDisposable
    {
        private readonly string _name;

        private EnvironmentVariableScope(string name, string value)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));

            Environment.SetEnvironmentVariable(_name, value, EnvironmentVariableTarget.Process);
        }

        public static IDisposable Create(string name, string value)
        {
            return new EnvironmentVariableScope(name, value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_name, null, EnvironmentVariableTarget.Process);
        }
    }
}