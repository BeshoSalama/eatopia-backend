using Eatopia.Api.Common;
using Eatopia.Api.Hubs;
using Eatopia.Api.Middlewares;
using Eatopia.Api.Serialization;
using Eatopia.Api.Swagger;
using Eatopia.Application.Interfaces;
using Eatopia.Infrastructure.AI;
using Eatopia.Infrastructure.Persistence;
using Eatopia.Infrastructure.Security;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.FileProviders;
using System.Text;
using System.Text.Json.Serialization;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/eatopia-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
ApplyDeploymentEnvironmentAliases(builder.Configuration);
builder.Host.UseSerilog();

var isTesting = builder.Environment.IsEnvironment("Testing");

// Controllers + JSON
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
        options.JsonSerializerOptions.DictionaryKeyPolicy = new SnakeCaseNamingPolicy();
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new FlexibleTimeSpanJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableFlexibleTimeSpanJsonConverter());
    });

// Make model validation errors follow our standard error shape
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        return new BadRequestObjectResult(new
        {
            success = false,
            message = "Invalid input",
            errors,
            error = new
            {
                code = "VALIDATION_ERROR",
                message = "Invalid input",
                details = errors
            }
        });
    };
});

builder.Services.AddEndpointsApiExplorer();

// Swagger + JWT button
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Eatopia API",
        Version = "v1",
        Description = "Backend for Eatopia React frontend. Supports both /api frontend-compatible endpoints and /api/v1 versioned endpoints."
    });

    c.OperationFilter<SwaggerExamplesOperationFilter>();

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// EF Core
builder.Services.AddDbContext<EatopiaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// JWT Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };

        // Allow JWT token in query string for SignalR WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var tokenVersionValue = context.Principal?.FindFirst("token_version")?.Value;

                if (!Guid.TryParse(userIdValue, out var userId) || !int.TryParse(tokenVersionValue, out var tokenVersion))
                {
                    context.Fail("Invalid token.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<EatopiaDbContext>();
                var currentUser = await db.Users
                    .Where(x => x.Id == userId)
                    .Select(x => new { x.JwtTokenVersion, x.Role, x.IsBanned })
                    .FirstOrDefaultAsync();

                if (currentUser == null || currentUser.JwtTokenVersion != tokenVersion || currentUser.IsBanned)
                {
                    context.Fail("Token has been revoked.");
                    return;
                }

                if (context.Principal?.Identity is System.Security.Claims.ClaimsIdentity identity)
                {
                    foreach (var claim in identity.FindAll(System.Security.Claims.ClaimTypes.Role).ToList())
                    {
                        identity.RemoveClaim(claim);
                    }

                    identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, currentUser.Role));
                }
            }
        };
    });

builder.Services.AddAuthorization();

// SignalR
builder.Services.AddSignalR();

// CORS
var configuredOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? [];

var frontendBaseUrl = builder.Configuration["Frontend:BaseUrl"]?.Trim().TrimEnd('/');
if (!string.IsNullOrWhiteSpace(frontendBaseUrl) && !configuredOrigins.Contains(frontendBaseUrl, StringComparer.OrdinalIgnoreCase))
{
    configuredOrigins = configuredOrigins.Append(frontendBaseUrl).ToArray();
}

if (!builder.Environment.IsDevelopment() && !isTesting)
{
    var jwtKey = builder.Configuration["Jwt:Key"] ?? "";
    if (jwtKey.Length < 32 ||
        jwtKey.Contains("Development", StringComparison.OrdinalIgnoreCase) ||
        jwtKey.Contains("CHANGE_THIS", StringComparison.OrdinalIgnoreCase) ||
        jwtKey.Contains("YOUR_", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Production requires a strong Jwt:Key. Set Jwt__Key in the host environment variables.");
    }

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
    if (string.IsNullOrWhiteSpace(connectionString) ||
        connectionString.Contains("Server=.;", StringComparison.OrdinalIgnoreCase) ||
        connectionString.Contains("YOUR_SQL", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Production requires a real SQL Server connection string. Set ConnectionStrings__DefaultConnection.");
    }

    if (configuredOrigins.Any(origin => origin.Contains("localhost", StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException("Production CORS origins cannot point to localhost. Set Cors__AllowedOrigins__0 and Frontend__BaseUrl to the public domain.");
    }

    if (configuredOrigins.Any(origin => origin == "*" || origin.Contains("*", StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException("Production CORS origins must be explicit. Do not use '*' in Cors:AllowedOrigins.");
    }

    if (configuredOrigins.Length == 0)
    {
        throw new InvalidOperationException("Production requires at least one CORS origin. Set FRONTEND_URL or Cors__AllowedOrigins__0.");
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();

        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true);
        }
        else if (configuredOrigins.Length > 0)
        {
            policy.WithOrigins(configuredOrigins);
        }
        else
        {
            policy.SetIsOriginAllowed(_ => false);
        }
    });
});

// Dependency Injection (Services)
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ExternalAuthService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<UserPreferencesService>();
builder.Services.AddScoped<FoodService>();
builder.Services.AddScoped<MealService>();
builder.Services.AddScoped<WaterService>();
builder.Services.AddScoped<MedicationService>();
builder.Services.AddScoped<RecipeService>();
builder.Services.AddScoped<CommunityService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<DietPlanService>();

// Background reminders: sends due Water/Medication reminders to in-app notifications and user email.
if (!isTesting)
{
    builder.Services.AddHostedService<ReminderNotificationBackgroundService>();
}

// AI Client: bridges the ASP.NET API to the Python models in the repository-level ai folder.
builder.Services.AddScoped<IFoodAiClient, PythonAiClient>();

var app = builder.Build();

app.UseSerilogRequestLogging();

// Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global error handling (our standard error format)
app.UseMiddleware<ExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Serve uploaded images/videos/voice-notes from a stable local folder first,
// then from project/bin wwwroot/uploads as backwards-compatible fallbacks.
// This prevents media from disappearing when you replace the project folder with a newer ZIP.
var uploadRoots = UploadStorageHelper.GetUploadRoots(app.Environment, app.Configuration);
foreach (var uploadRoot in uploadRoots)
{
    Directory.CreateDirectory(uploadRoot);
    Log.Information("Upload static root active: {UploadRoot}", uploadRoot);
}

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = UploadStorageHelper.UploadsRequestPath,
    FileProvider = UploadStorageHelper.BuildCompositeUploadFileProvider(app.Environment, app.Configuration)
});

app.UseCors("AppCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

var webRootPath = app.Environment.WebRootPath;
if (string.IsNullOrWhiteSpace(webRootPath))
{
    webRootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
}

if (File.Exists(Path.Combine(webRootPath, "index.html")))
{
    app.MapFallbackToFile("index.html");
}

// Create DB + seed in Development for easy demo.
// Integration tests own their SQLite schema, so startup migration/repair is skipped there.
if (!isTesting)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<EatopiaDbContext>();

        // Apply migrations automatically, then run an idempotent schema repair.
        // If your local database has a half-applied migration, do not stop the app here;
        // the repair step below creates/adds the missing tables and columns safely.
        try
        {
            //db.Database.Migrate();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "EF migration failed. Continuing with DatabaseSchemaRepair because the local database may be partially migrated.");
        }

        //try
	//{
	//    DatabaseSchemaRepair.Apply(db);
	//    CommunityAuthSchemaRepair.Apply(db);
	//}
	//catch (Exception ex)
	//{
	//    Log.Error(ex, "Database schema repair failed. Check SQL Server connectivity and permissions.");
	//}

        if (app.Environment.IsDevelopment())
        {
            try
            {
                DbSeeder.Seed(db);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Database seed failed. The API will continue running so you can inspect the real startup error.");
            }
        }
        else if (app.Configuration.GetValue<bool>("Seed:Owner:Enabled"))
        {
            var ownerEmail = app.Configuration["Seed:Owner:Email"] ?? "";
            var ownerPassword = app.Configuration["Seed:Owner:Password"] ?? "";
            var ownerUsername = app.Configuration["Seed:Owner:Username"] ?? "owner";
            var ownerName = app.Configuration["Seed:Owner:Name"] ?? "Eatopia Owner";

            DbSeeder.SeedOwner(db, ownerEmail, ownerPassword, ownerUsername, ownerName);
            Log.Information("Production owner seed checked for {OwnerEmail}. Disable Seed:Owner:Enabled after first successful deployment.", ownerEmail);
        }
    }
}

app.Run();

static void ApplyDeploymentEnvironmentAliases(ConfigurationManager configuration)
{
    SetIfPresent(configuration, "API_BASE_URL", "Api:BaseUrl");
    SetIfPresent(configuration, "FRONTEND_URL", "Frontend:BaseUrl");
    SetIfPresent(configuration, "AI_SERVICE_URL", "AI:ServiceUrl");

    var corsOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
    if (!string.IsNullOrWhiteSpace(corsOrigins))
    {
        var origins = corsOrigins
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(origin => origin.TrimEnd('/'))
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        for (var i = 0; i < origins.Length; i++)
        {
            configuration[$"Cors:AllowedOrigins:{i}"] = origins[i];
        }
    }

    var wwwFrontendUrl = Environment.GetEnvironmentVariable("WWW_FRONTEND_URL")?.Trim().TrimEnd('/');
    if (!string.IsNullOrWhiteSpace(wwwFrontendUrl) && string.IsNullOrWhiteSpace(configuration["Cors:AllowedOrigins:1"]))
    {
        configuration["Cors:AllowedOrigins:1"] = wwwFrontendUrl;
    }
}

static void SetIfPresent(ConfigurationManager configuration, string environmentName, string configurationKey)
{
    var value = Environment.GetEnvironmentVariable(environmentName);
    if (!string.IsNullOrWhiteSpace(value))
    {
        configuration[configurationKey] = value.Trim().TrimEnd('/');
    }
}

public partial class Program
{
}
