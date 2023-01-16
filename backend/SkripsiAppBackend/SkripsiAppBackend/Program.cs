using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using SkripsiAppBackend.Common;
using SkripsiAppBackend.Services;

var builder = WebApplication.CreateBuilder(args);

var applicationConfiguration = new Configuration(
    jwtSigningSecret: Environment.GetEnvironmentVariable("JWT_SIGNING_SECRET"),
    clientAppId: Environment.GetEnvironmentVariable("OAUTH_CLIENT_APP_ID"),
    authUrl: Environment.GetEnvironmentVariable("OAUTH_AUTH_URL"),
    scope: Environment.GetEnvironmentVariable("OAUTH_SCOPE"),
    callbackUrl: Environment.GetEnvironmentVariable("OAUTH_CALLBACK_URL"),
    tokenUrl: Environment.GetEnvironmentVariable("OAUTH_TOKEN_URL"),
    clientAppSecret: Environment.GetEnvironmentVariable("OAUTH_CLIENT_APP_SECRET"),
    environment: Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(applicationConfiguration);
builder.Services.AddSingleton<IKeyValueService>(new InMemoryKeyValueService());

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

app.UseAuthorization();

//app.MapControllers();

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
