using System.Collections.Generic;
using Ficha_Tecnica.Data;
using Ficha_Tecnica.Services;
using Google.Cloud.Diagnostics.AspNetCore3;
using Google.Cloud.Diagnostics.Common;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var googleCloudSection = builder.Configuration.GetSection("GoogleCloud");
var googleProjectId = googleCloudSection.GetValue<string>("ProjectId");
if (string.IsNullOrWhiteSpace(googleProjectId))
{
    googleProjectId = builder.Configuration["GOOGLE_CLOUD_PROJECT"];
}

if (string.IsNullOrWhiteSpace(googleProjectId))
{
    googleProjectId = builder.Configuration["GOOGLE_PROJECT_ID"];
}

var googleLogName = googleCloudSection.GetValue<string>("LogName");
var normalizedGoogleLogName = string.IsNullOrWhiteSpace(googleLogName)
    ? "application"
    : googleLogName.Trim();

var googleLoggingEnabled = false;

if (!string.IsNullOrWhiteSpace(googleProjectId))
{
    builder.Services.AddGoogleDiagnosticsForAspNetCore(
        projectId: googleProjectId,
        serviceName: "ficha-tecnica",
        serviceVersion: builder.Environment.EnvironmentName);

    builder.Logging.AddGoogle(new LoggingServiceOptions
    {
        ProjectId = googleProjectId,
        Options = LoggingOptions.Create(
            logName: normalizedGoogleLogName)
    });

    googleLoggingEnabled = true;
}

builder.Services.AddHttpClient();

// Add services to the container.
builder.Services.AddMemoryCache();
builder.Services.AddRazorPages(options =>
{
    // Define the login page as the startup (root) route.
    options.Conventions.AddPageRoute("/Login", "");
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = builder.Configuration["DB_CONNECTION"];
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "A connection string named 'DefaultConnection' must be configured via configuration files or the DB_CONNECTION environment variable.");
}

var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString);
if (connectionStringBuilder.SslMode == MySqlSslMode.None)
{
    connectionStringBuilder.SslMode = MySqlSslMode.Required;
}

connectionString = connectionStringBuilder.ConnectionString;

var recipeImageStorageSection = builder.Configuration.GetSection(RecipeImageStorageOptions.SectionName);
var recipeImageStorageOptions = recipeImageStorageSection.Get<RecipeImageStorageOptions>();

builder.Services.Configure<RecipeImageStorageOptions>(recipeImageStorageSection);

var serverConnectionProbeSection = builder.Configuration.GetSection(ServerConnectionProbeOptions.SectionName);
builder.Services.Configure<ServerConnectionProbeOptions>(options =>
{
    serverConnectionProbeSection.Bind(options);

    if (string.IsNullOrWhiteSpace(options.Url))
    {
        var fallbackUrl = builder.Configuration["SERVER_PROBE_URL"];
        if (!string.IsNullOrWhiteSpace(fallbackUrl))
        {
            options.Url = fallbackUrl;
        }
    }
});

var databaseConnectionProbeSection = builder.Configuration.GetSection(DatabaseConnectionProbeOptions.SectionName);
builder.Services.Configure<DatabaseConnectionProbeOptions>(options =>
{
    databaseConnectionProbeSection.Bind(options);
    options.ConnectionString = connectionString;
});

builder.Services.AddHostedService<ServerConnectionProbeService>();
builder.Services.AddHostedService<DatabaseConnectionProbeService>();

builder.Services.AddScoped<IUserRepository>(_ => new UserRepository(connectionString));
builder.Services.AddScoped<ICategoryRepository>(_ => new CategoryRepository(connectionString));
builder.Services.AddScoped<IIngredientRepository>(_ => new IngredientRepository(connectionString));
builder.Services.AddScoped<ISupplierRepository>(_ => new SupplierRepository(connectionString));
builder.Services.AddScoped<IRecipeCategoryRepository>(_ => new RecipeCategoryRepository(connectionString));
builder.Services.AddScoped<IRecipeRepository>(_ => new RecipeRepository(connectionString));
builder.Services.AddScoped<IPriceMovementRepository>(_ => new PriceMovementRepository(connectionString));
builder.Services.AddSingleton<IRecipePdfExporter, RecipePdfExporter>();
builder.Services.AddSingleton<IDashboardReportPdfExporter, DashboardReportPdfExporter>();
builder.Services.AddSingleton<ILoginRateLimiter, LoginRateLimiter>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<IMalwareScanner, SignatureMalwareScanner>();
builder.Services.AddSingleton<IRecipeImageStorage, RecipeImageStorage>();

var app = builder.Build();

var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var startupLogger = loggerFactory.CreateLogger("Startup");

if (googleLoggingEnabled)
{
    startupLogger.LogInformation(
        "Google Cloud logging configured for project {ProjectId} with log name {LogName}.",
        googleProjectId,
        normalizedGoogleLogName);
}
else
{
    startupLogger.LogWarning(
        "Google Cloud logging is not configured. Set GoogleCloud:ProjectId in appsettings.json (e.g. Ficha Tecnica/appsettings.json or appsettings.{Environment}.json) or assign the GOOGLE_CLOUD_PROJECT/GOOGLE_PROJECT_ID environment variable to enable cloud logs.");
}

var applicationLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
applicationLifetime.ApplicationStarted.Register(() =>
    startupLogger.LogInformation(
        "Application started at {TimestampUtc} in {Environment} environment.",
        DateTimeOffset.UtcNow,
        app.Environment.EnvironmentName));

applicationLifetime.ApplicationStopping.Register(() =>
    startupLogger.LogInformation(
        "Application stopping at {TimestampUtc}.",
        DateTimeOffset.UtcNow));

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

var imageSources = new List<string>
{
    "'self'",
    "data:",
};

static void TryAddImageSource(ICollection<string> sources, string? source)
{
    if (string.IsNullOrWhiteSpace(source))
    {
        return;
    }

    if (!sources.Contains(source))
    {
        sources.Add(source);
    }
}

// Allow Google Cloud Storage hosted recipe images while keeping a restrictive default policy.
TryAddImageSource(imageSources, "https://storage.googleapis.com");

if (!string.IsNullOrWhiteSpace(recipeImageStorageOptions?.BucketName))
{
    var bucketName = recipeImageStorageOptions.BucketName.Trim();
    if (!string.IsNullOrEmpty(bucketName))
    {
        TryAddImageSource(imageSources, $"https://{bucketName}.storage.googleapis.com");
    }
}

var contentSecurityPolicy = string.Join("; ", new[]
{
    "default-src 'self'",
    $"img-src {string.Join(' ', imageSources)}",
    "script-src 'self'",
    "style-src 'self' 'unsafe-inline'",
    "font-src 'self' data:",
    "connect-src 'self'",
    "frame-ancestors 'none'",
    "form-action 'self'",
    "base-uri 'self'",
    "object-src 'none'",
});

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("Content-Security-Policy", contentSecurityPolicy);
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    await next();
});

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
