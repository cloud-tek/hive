using System.Collections.Generic;

namespace Ion.Testing
{
    public sealed class SmartFactAttribute : Xunit.FactAttribute
    {
        public SmartFactAttribute(On on)
        {
            Skip = TestExecutionResolver.Resolve(on);
        }

        public SmartFactAttribute(Execute execute)
        {
            Skip = TestExecutionResolver.Resolve(execute);
        }

        public SmartFactAttribute(Execute execute, On on)
        {
            Skip = TestExecutionResolver.Resolve(execute, on);
        }

        public SmartFactAttribute(Execute execute, On on, params string[] environment)
        {
            Skip = TestExecutionResolver.Resolve(execute, on, environment);
        }
    }
}