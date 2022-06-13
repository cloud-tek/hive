namespace Ion.Configuration.Validation;

public class ValidationResult
{
    public string? ErrorMessage { get; set; }

    public string[] MemberNames { get; set; } = Array.Empty<string>();

    public ValidationResult[] ValidationResults { get; set; } = Array.Empty<ValidationResult>();

    public override string ToString()
    {
        var memberNames = string.Join(";", MemberNames);
        return $"{memberNames} => {ErrorMessage}";
    }
}