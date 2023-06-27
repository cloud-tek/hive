namespace Hive.Configuration.Validation;

public class CompositeDataAnnotationsValidationResult : global::System.ComponentModel.DataAnnotations.ValidationResult
{
    private readonly List<System.ComponentModel.DataAnnotations.ValidationResult> results = new();

    public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Results => results;

    public CompositeDataAnnotationsValidationResult(string errorMessage) : base(errorMessage)
    {
    }

    public CompositeDataAnnotationsValidationResult(string errorMessage, IEnumerable<string> memberNames) : base(errorMessage, memberNames)
    {
    }

    protected CompositeDataAnnotationsValidationResult(System.ComponentModel.DataAnnotations.ValidationResult validationResult) : base(validationResult)
    {
    }

    public void AddResult(System.ComponentModel.DataAnnotations.ValidationResult validationResult)
    {
        results.Add(validationResult);
    }
}
