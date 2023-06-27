namespace Hive.Configuration.Validation;

public class CompositeFluentValidationResult : global::FluentValidation.Results.ValidationResult
{
    // private readonly List<System.ComponentModel.DataAnnotations.ValidationResult> results = new();

    // public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Results => results;

    // public CompositeFluentValidationResult(string errorMessage) : base(errorMessage)
    // {
    // }
    //
    // public CompositeFluentValidationResult(string errorMessage, IEnumerable<string> memberNames) : base(errorMessage, memberNames)
    // {
    // }

    protected CompositeFluentValidationResult(FluentValidation.Results.ValidationResult validationResult)// : base(validationResult)
    {
    }

    public void AddResult(FluentValidation.Results.ValidationResult validationResult)
    {
        //results.Add(validationResult);
    }
}
