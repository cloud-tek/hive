using System.ComponentModel.DataAnnotations;

namespace Ion.Configuration.Validation;

public static class Validator
{
    public static ValidationResult[] ValidateReturnValue(object objectToValidate)
    {
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        if (objectToValidate == null)
        {
            validationResults.Add(new System.ComponentModel.DataAnnotations.ValidationResult("Return value is required."));
        }
        else
        {
            var validationContext = new ValidationContext(objectToValidate);

            System.ComponentModel.DataAnnotations.Validator.TryValidateObject(objectToValidate, validationContext, validationResults, true);

            if (validationResults.Count != 0)
            {
                var compositeValidationResult = new CompositeValidationResult($"Validation for {validationContext.DisplayName} failed.", new[] { validationContext.MemberName });
                validationResults.ForEach(compositeValidationResult.AddResult);
            }
        }

        var structuredValidationResults = StructureValidationResults(validationResults);
        return structuredValidationResults;
    }

    private static ValidationResult[] StructureValidationResults(IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> validationResults)
    {
        var structuredValidationResults = new List<ValidationResult>();
        foreach (var validationResult in validationResults)
        {
            var structuredValidationResult = new ValidationResult
            {
                ErrorMessage = validationResult.ErrorMessage,
                MemberNames = validationResult.MemberNames.ToArray()
            };

            if (validationResult is CompositeValidationResult compositeValidationResult)
            {
                structuredValidationResult.ValidationResults = StructureValidationResults(compositeValidationResult.Results);
            }

            structuredValidationResults.Add(structuredValidationResult);
        }

        return structuredValidationResults.ToArray();
    }
}