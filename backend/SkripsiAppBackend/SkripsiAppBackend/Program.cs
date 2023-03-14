using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using SkripsiAppBackend.Common;
using SkripsiAppBackend.Common.Authentication;
using SkripsiAppBackend.Common.Authorization;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Services;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.Services.ObjectCachingService;
using SkripsiAppBackend.UseCases;

var builder = WebApplication.CreateBuilder(args);

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
    timelinessMarginFactor: 0.5
);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(applicationConfiguration);
builder.Services.AddSingleton<IKeyValueService, InMemoryKeyValueService>();
builder.Services.AddSingleton<AccessTokenService>();

builder.Services.AddScoped<TeamUseCases>();
builder.Services.AddScoped<ReportUseCases>();

builder.Services.AddInMemoryObjectCaching(
    typeof(List<AuthenticationMiddleware.ProfileTeam>)
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
    options.AddPolicy(AuthorizationPolicies.AllowTeamMember,
        policy => policy.Requirements.Add(new AllowTeamMemberRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, AllowTeamMemberHandler>();

var app = builder.Build();

app.UseRouting();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (applicationConfiguration.Environment == Configuration.ExecutionEnvironment.Production)
{
    app.UseHttpsRedirection();
}

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
