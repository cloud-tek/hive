using System;

namespace Ion.Testing
{
    [Flags]
    public enum On
    {
        All = 0,
        Windows = 1,
        Linux = 2,
        MacOS = 4
    }
}