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
        Options = LoggingOptions.Create(logName: normalizedGoogleLogName)
    });

    googleLoggingEnabled = true;
}

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

builder.Services.AddRazorPages(options =>
{
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
    throw new InvalidOperationException("Configure a connection string.");
}

var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString);
if (connectionStringBuilder.SslMode == MySqlSslMode.None)
{
    connectionStringBuilder.SslMode = MySqlSslMode.Required;
}
connectionString = connectionStringBuilder.ConnectionString;

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

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();


// 🔥🔥🔥 CRIAR ADMIN AUTOMÁTICO NO BANCO 🔥🔥🔥
// só cria se não existir
using (var scope = app.Services.CreateScope())
{
    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ADMIN-SEED");

    try
    {
        var existing = await userRepository.GetByUsernameAsync("admin");

        if (existing == null)
        {
            var (hash, salt) = passwordHasher.HashPassword("123456"); // <<< TROQUE A SENHA

            var user = new UserAccount
            {
                Username = "admin",
                Email = "admin@admin.com",
                PasswordHash = hash,
                Salt = salt
            };

            await userRepository.CreateUserAsync(user);
            logger.LogInformation("ADMIN CRIADO COM SUCESSO 🔐");
        }
        else
        {
            logger.LogInformation("ADMIN JA EXISTE");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "ERRO AO CRIAR ADMIN");
    }
}

app.Run();
