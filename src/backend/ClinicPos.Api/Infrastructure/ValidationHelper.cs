using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ClinicPos.Api.Infrastructure;

public static class ValidationHelper
{
    public static Dictionary<string, string[]>? Validate<T>(T model)
    {
        var context = new ValidationContext(model!);
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(model!, context, results, true))
        {
            return null;
        }

        return results
            .SelectMany(result => result.MemberNames.Select(member => (member, message: result.ErrorMessage ?? "Invalid value")))
            .GroupBy(x => x.member)
            .ToDictionary(x => x.Key, x => x.Select(y => y.message).ToArray());
    }

    public static IResult ValidationProblem(Dictionary<string, string[]> errors) =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "ValidationError",
            extensions: new Dictionary<string, object?> { ["errors"] = errors });
}
