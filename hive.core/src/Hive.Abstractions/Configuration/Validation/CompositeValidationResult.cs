// namespace Hive.Configuration.Validation;
//
// public class CompositeValidationResult : global::System.ComponentModel.DataAnnotations.ValidationResult
// {
//     private readonly List<System.ComponentModel.DataAnnotations.ValidationResult> results = new();
//
//     public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Results => results;
//
//     public CompositeValidationResult(string errorMessage) : base(errorMessage)
//     {
//     }
//
//     public CompositeValidationResult(string errorMessage, IEnumerable<string> memberNames) : base(errorMessage, memberNames)
//     {
//     }
//
//     protected CompositeValidationResult(System.ComponentModel.DataAnnotations.ValidationResult validationResult) : base(validationResult)
//     {
//     }
//
//     public void AddResult(System.ComponentModel.DataAnnotations.ValidationResult validationResult)
//     {
//         results.Add(validationResult);
//     }
// }
