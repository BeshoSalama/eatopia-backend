using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Eatopia.Api.Swagger;

public class SwaggerExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        AddResponseIfMissing(operation, "400", "Bad request / validation error");
        AddResponseIfMissing(operation, "401", "Unauthorized - missing or invalid JWT");
        AddResponseIfMissing(operation, "500", "Unexpected server error");

        var path = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? string.Empty;

        if (path == "api/login" || path == "api/v1/auth/login")
        {
            AddJsonRequestExample(operation, new OpenApiObject
            {
                ["usernameOrEmail"] = new OpenApiString("amir@example.com"),
                ["password"] = new OpenApiString("Password123")
            });

            AddJsonResponseExample(operation, "200", new OpenApiObject
            {
                ["success"] = new OpenApiBoolean(true),
                ["message"] = new OpenApiString("Logged in successfully."),
                ["token"] = new OpenApiString("eyJhbGciOi..."),
                ["user"] = new OpenApiObject
                {
                    ["id"] = new OpenApiString("00000000-0000-0000-0000-000000000000"),
                    ["fullName"] = new OpenApiString("Amir Hany"),
                    ["username"] = new OpenApiString("amir"),
                    ["email"] = new OpenApiString("amir@example.com"),
                    ["role"] = new OpenApiString("User")
                }
            });
        }

        if (path == "api/signup" || path == "api/v1/auth/register")
        {
            AddJsonRequestExample(operation, new OpenApiObject
            {
                ["fullName"] = new OpenApiString("Amir Hany"),
                ["username"] = new OpenApiString("amir"),
                ["email"] = new OpenApiString("amir@example.com"),
                ["password"] = new OpenApiString("Password123"),
                ["birthDate"] = new OpenApiString("2003-01-15"),
                ["location"] = new OpenApiString("Cairo"),
                ["gender"] = new OpenApiString("male")
            });
        }

        if (path == "api/ai/diet-plan" || path == "api/v1/ai/diet-plan")
        {
            AddJsonRequestExample(operation, new OpenApiObject
            {
                ["allergies"] = new OpenApiArray { new OpenApiString("milk"), new OpenApiString("peanuts") },
                ["avoidFoods"] = new OpenApiArray { new OpenApiString("fish") },
                ["durationDays"] = new OpenApiInteger(7),
                ["mealsPerDay"] = new OpenApiArray
                {
                    new OpenApiString("breakfast"),
                    new OpenApiString("lunch"),
                    new OpenApiString("dinner"),
                    new OpenApiString("snacks")
                },
                ["preferences"] = new OpenApiObject
                {
                    ["goal"] = new OpenApiString("healthy balanced diet"),
                    ["language"] = new OpenApiString("en")
                }
            });

            AddJsonResponseExample(operation, "200", new OpenApiObject
            {
                ["weeklyPlan"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["day"] = new OpenApiInteger(1),
                        ["meals"] = new OpenApiObject
                        {
                            ["breakfast"] = new OpenApiObject
                            {
                                ["title"] = new OpenApiString("Breakfast"),
                                ["text"] = new OpenApiString("Oatmeal with banana\nCinnamon\nBoiled egg\nWater")
                            }
                        }
                    }
                }
            });
        }
    }

    private static void AddResponseIfMissing(OpenApiOperation operation, string statusCode, string description)
    {
        if (!operation.Responses.ContainsKey(statusCode))
            operation.Responses.Add(statusCode, new OpenApiResponse { Description = description });
    }

    private static void AddJsonRequestExample(OpenApiOperation operation, IOpenApiAny example)
    {
        if (operation.RequestBody?.Content.TryGetValue("application/json", out var mediaType) == true)
            mediaType.Example = example;
    }

    private static void AddJsonResponseExample(OpenApiOperation operation, string statusCode, IOpenApiAny example)
    {
        if (!operation.Responses.TryGetValue(statusCode, out var response))
            return;

        response.Content ??= new Dictionary<string, OpenApiMediaType>();

        if (!response.Content.TryGetValue("application/json", out var mediaType))
        {
            mediaType = new OpenApiMediaType();
            response.Content["application/json"] = mediaType;
        }

        mediaType.Example = example;
    }
}
