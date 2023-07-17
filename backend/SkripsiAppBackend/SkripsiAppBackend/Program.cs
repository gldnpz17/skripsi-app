using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Https;
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
using SkripsiAppBackend.Services.UniversalCachingService;
using SkripsiAppBackend.UseCases;
using System.Net;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default,
    WebRootPath = $"{AppContext.BaseDirectory}/frontend"
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
    enableTls: Environment.GetEnvironmentVariable("TLS_ENABLED"),
    useBuiltFrontend: Environment.GetEnvironmentVariable("USE_BUILT_FRONTEND"),
    tlsCertificatePath: Environment.GetEnvironmentVariable("TLS_CERTIFICATE_PATH"),
    tlsPrivateKeyPath: Environment.GetEnvironmentVariable("TLS_PRIVATE_KEY_PATH")
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
builder.Services.AddSingleton<IDateTimeService>(_ =>
{
    if (applicationConfiguration.Environment == Configuration.ExecutionEnvironment.Development)
    {
        return new MockDateTimeService();
    }
    else
    {
        return new WestIndonesianDateTimeService();
    }
});

builder.Services.AddSingleton((service) => new InMemoryUniversalCachingService(TimeSpan.FromSeconds(10)));

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

Database.Migrate(applicationConfiguration.ConnectionString);
builder.Services.AddScoped((service) =>
{
    var cache = service.GetRequiredService<InMemoryUniversalCachingService>();
    return new Database(applicationConfiguration.ConnectionString).AddCache(cache);
});

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

if (
    applicationConfiguration.UseBuiltFrontend ||
    applicationConfiguration.Environment == Configuration.ExecutionEnvironment.Production
)
{
    builder.Services.AddSpaStaticFiles(options =>
    {
        options.RootPath = $"{AppContext.BaseDirectory}/frontend";
    });
}

builder.Host.UseWindowsService();

if (
    applicationConfiguration.EnableTls ||
    applicationConfiguration.Environment == Configuration.ExecutionEnvironment.Production)
{
    var cert = X509Certificate2.CreateFromPemFile(
        applicationConfiguration.TlsCertificatePath,
        applicationConfiguration.TlsPrivateKeyPath);

    // Bloody fucking hell. I've spent an entire day wondering why the HTTPS connection kept failing.
    // Error logs hidden by default. I've set the logging level to trace.
    // See : https://github.com/dotnet/runtime/issues/45680
    cert = new X509Certificate2(cert.Export(X509ContentType.Pkcs12));

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

if (
    applicationConfiguration.EnableTls ||
    applicationConfiguration.Environment == Configuration.ExecutionEnvironment.Production)
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

if (
    applicationConfiguration.UseBuiltFrontend ||
    applicationConfiguration.Environment == Configuration.ExecutionEnvironment.Production
)
{
    var options = new DefaultFilesOptions();
    options.DefaultFileNames.Clear();
    options.DefaultFileNames.Add("index.html");
    app.UseDefaultFiles(options);

    app.UseStaticFiles();
    app.UseSpaStaticFiles();
}

app.UseRouting();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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
    if (applicationConfiguration.Environment == Configuration.ExecutionEnvironment.Development)
    {
        config.Options.SourcePath = "./../../../frontend/";
        config.UseReactDevelopmentServer(npmScript: "start");
    }
});

await app.RunAsync();
