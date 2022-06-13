using System;

namespace Ion.Testing
{
    [Flags]
    public enum Execute
    {
        Always = 0,
        InGithubActions = 1,
        InAzureDevOps = 2,
        InContainer = 3,
        InDebug = 4
    }
}