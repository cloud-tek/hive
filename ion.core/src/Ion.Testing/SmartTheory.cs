namespace Ion.Testing
{
    public sealed class SmartTheoryAttribute : Xunit.TheoryAttribute
    {
        public SmartTheoryAttribute(On on)
        {
            Skip = TestExecutionResolver.Resolve(on);
        }

        public SmartTheoryAttribute(Execute execute)
        {
            Skip = TestExecutionResolver.Resolve(execute);
        }

        public SmartTheoryAttribute(Execute execute, On on)
        {
            Skip = TestExecutionResolver.Resolve(execute, on);
        }
    }
}