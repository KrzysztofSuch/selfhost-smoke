using Npgsql;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var postgres = builder.Configuration.GetConnectionString("Postgres")!;
var redis = builder.Configuration.GetConnectionString("Redis")!;

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(postgres));
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect($"{redis},abortConnect=false"));

var app = builder.Build();

app.MapGet("/", () => new { version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "dev" });

app.MapGet("/health/ready", async (NpgsqlDataSource db, IConnectionMultiplexer cache) =>
{
    try
    {
        await using var command = db.CreateCommand("select 1");
        await command.ExecuteScalarAsync();
        await cache.GetDatabase().PingAsync();
        return Results.Ok(new { status = "ready" });
    }
    catch (Exception error)
    {
        return Results.Json(new { status = "not ready", error = error.GetType().Name }, statusCode: 503);
    }
});

app.Run();
