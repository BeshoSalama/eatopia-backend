using Eatopia.Infrastructure.Security;
using Eatopia.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Eatopia.Tests.Support;

public static class TestServices
{
    public static IConfiguration Configuration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-secret-key-for-eatopia-jwt-tests-1234567890",
                ["Jwt:Issuer"] = "Eatopia.Tests",
                ["Jwt:Audience"] = "Eatopia.Tests",
                ["Jwt:DurationInMinutes"] = "60",
                ["Frontend:BaseUrl"] = "http://localhost:3000",
                ["Email:BypassWhenMissingInDevelopment"] = "true",
                ["Email:Password"] = "PUT_GMAIL_APP_PASSWORD_HERE"
            })
            .Build();

    public static AuthService AuthService(TestDatabase database)
    {
        var configuration = Configuration();
        return new AuthService(
            database.Context,
            new JwtService(configuration),
            new EmailService(configuration, NullLogger<EmailService>.Instance),
            configuration);
    }
}
