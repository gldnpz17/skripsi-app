using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.WindowsServices;
using SkripsiAppBackend.Calculations;
using SkripsiAppBackend.Common;
using SkripsiAppBackend.Common.Authentication;
using SkripsiAppBackend.Common.Authorization;
using SkripsiAppBackend.Common.Middlewares;
using SkripsiAppBackend.Common.ModelBinder;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Services;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.Services.DateTimeService;
using SkripsiAppBackend.Services.LoggingService;
using SkripsiAppBackend.Services.ObjectCachingService;
using SkripsiAppBackend.UseCases;
using System.Net;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
});

var applicationConfiguration = new Configuration(
    jwtSigningSecret: Environment.GetEnvironmentVariable("JWT_SIGNING_SECRET"),
    clientAppId: Environment.GetEnvironmentVariable("OAUTH_CLIENT_APP_ID"),
    authUrl: Environment.GetEnvironmentVariable("OAUTH_AUTH_URL"),
    scope: Environment.GetEnvironmentVariable("OAUTH_SCOPE"),
    callbackUrl: Environment.GetEnvironmentVariable("OAUTH_CALLBACK_URL"),
    tokenUrl: Environment.GetEnvironmentVariable("OAUTH_TOKEN_URL"),
    clientAppSecret: Environment.GetEnvironmentVariable("OAUTH_CLIENT_APP_SECRET"),
    environment: Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    connectionString: Environment.GetEnvironmentVariable("CONNECTION_STRING"),
    accessTokenLifetime: TimeSpan.FromSeconds(3),
    timelinessMarginFactor: 0.5,
    enableTls: Environment.GetEnvironmentVariable("TLS_ENABLED")
);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new RelativeDateTimeModelBinder.Provider());
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(applicationConfiguration);
builder.Services.AddSingleton<IKeyValueService, InMemoryKeyValueService>();
builder.Services.AddScoped<AccessTokenService>();
builder.Services.AddSingleton<IDateTimeService, MockDateTimeService>();

builder.Services.AddSingleton((service) => new InMemoryUniversalCachingService(TimeSpan.FromSeconds(5)));

builder.Services.AddSingleton((service) =>
{
    return new LoggingService
    {
        Strategy = new ConsoleLoggingStrategy()
    };
});

builder.Services.AddScoped<ReportCalculations>();
builder.Services.AddScoped<CommonCalculations>();
builder.Services.AddScoped<TeamEvmCalculations>();
builder.Services.AddScoped<TimeSeriesCalculations>();
builder.Services.AddScoped<MiscellaneousCalculations>();

builder.Services.AddInMemoryObjectCaching(
    typeof(List<AuthenticationMiddleware.ProfileTeam>),
    typeof(IAzureDevopsService.Team)
);

if (applicationConfiguration.Environment == Configuration.ExecutionEnvironment.Development)
{
    var database = new Database(applicationConfiguration.ConnectionString);
    database.Migrate();
    builder.Services.AddSingleton(database);
}

builder.Services.AddAzureDevopsService(AzureDevopsServiceType.REST);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AllowAuthenticated,
        policy => policy.Requirements.Add(new AllowAuthenticatedRequirement()));

    options.AddPolicy(AuthorizationPolicies.AllowTeamMember,
        policy => policy.Requirements.Add(new AllowTeamMemberRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, AllowTeamMemberHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, AllowAuthenticationHandler>();

builder.Host.UseWindowsService();

if (
    applicationConfiguration.EnableTls ||
    applicationConfiguration.Environment == Configuration.ExecutionEnvironment.Production)
{
    var certPath = AppContext.BaseDirectory + "./certificate.pem";
    var keyPath = AppContext.BaseDirectory + "./private_key.pem";
    var cert = X509Certificate2.CreateFromPemFile(certPath, keyPath);

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Listen(IPAddress.Any, 80); // Should redirect to port 443.
        options.Listen(IPAddress.Any, 443, listenOptions =>
        {
            listenOptions.UseHttps(cert);
        });
    });
}

var app = builder.Build();

app.UseRouting();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (
    applicationConfiguration.EnableTls ||
    applicationConfiguration.Environment == Configuration.ExecutionEnvironment.Production)
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseErrorHandling();

app.UseAzureDevopsService(AzureDevopsServiceType.REST);

app.UseAzureDevopsAuthentication();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.UseSpa(config =>
{
    config.Options.SourcePath = "./../../../frontend/";

    if (applicationConfiguration.Environment == Configuration.ExecutionEnvironment.Development)
    {
        config.UseReactDevelopmentServer(npmScript: "start");
    }
});

app.Run();
