using System.ComponentModel.DataAnnotations;

namespace Wib.Api.Common;

public static class ValidationEndpointFilterExtensions
{
    public static TBuilder WithValidation<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            foreach (var arg in context.Arguments)
            {
                if (arg is not IValidatableObject validatable) continue;
                
                var results = new List<ValidationResult>();
                var validationContext = new ValidationContext(validatable);
                if (Validator.TryValidateObject(validatable, validationContext, results, validateAllProperties: true))
                    continue;
                
                var errors = results
                    .GroupBy(r => r.MemberNames.FirstOrDefault() ?? string.Empty)
                    .ToDictionary(g => g.Key, g => g.Select(r => r.ErrorMessage ?? string.Empty).ToArray());

                return Results.ValidationProblem(errors);
            }

            return await next(context);
        });
    }
}
