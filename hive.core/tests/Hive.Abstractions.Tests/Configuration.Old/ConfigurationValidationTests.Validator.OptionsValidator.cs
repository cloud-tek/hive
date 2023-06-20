using System.Linq;
using System.Text;
using Microsoft.Extensions.Options;

namespace Hive.Tests.Configuration;

// public partial class ConfigurationValidationTests
// {
//   public partial class Validator
//   {
//     // ReSharper disable once ClassNeverInstantiated.Global
//     public class OptionsValidator : IValidateOptions<Options>
//     {
//       public ValidateOptionsResult Validate(string name, Options options)
//       {
//         var validationResults = global::Hive.Configuration.Validation.Validator.ValidateReturnValue(options);
//         if (validationResults.Any())
//         {
//           var builder = new StringBuilder();
//           foreach (var result in validationResults)
//           {
//             var pretty = PrettyPrint(result, string.Empty, true);
//             builder.Append(pretty);
//           }
//           return ValidateOptionsResult.Fail(builder.ToString());
//         }
//
//         return ValidateOptionsResult.Success;
//       }
//
//       private string PrettyPrint(global::Hive.Configuration.Validation.ValidationResult root, string indent, bool last)
//       {
//         // Based on https://stackoverflow.com/a/1649223
//         var sb = new StringBuilder();
//         sb.Append(indent);
//         if (last)
//         {
//           sb.Append("|-");
//           indent += "  ";
//         }
//         else
//         {
//           sb.Append("|-");
//           indent += "| ";
//         }
//
//         sb.AppendLine(root.ToString());
//
//         if (root.ValidationResults != null)
//         {
//           for (var i = 0; i < root.ValidationResults.Length; i++)
//           {
//             var child = root.ValidationResults[i];
//             var pretty = PrettyPrint(child, indent, i == root.ValidationResults.Length - 1);
//             sb.Append(pretty);
//           }
//         }
//
//         return sb.ToString();
//       }
//     }
//   }
// }
