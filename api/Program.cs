using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var configuredConnectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

string connectionString;
string dbHost;
bool usesSqlServer;

if (!string.IsNullOrWhiteSpace(configuredConnectionString))
{
    // Azure provides a SQL Server connection string.
    var connectionSettings = new SqlConnectionStringBuilder(configuredConnectionString)
    {
        Encrypt = true,
        TrustServerCertificate = false
    };
    connectionString = connectionSettings.ConnectionString;
    dbHost = connectionSettings.DataSource;
    usesSqlServer = true;
}
else
{
    // Local Compose provides separate Database__* settings.
    var database = builder.Configuration.GetSection("Database");
    var connectionSettings = new NpgsqlConnectionStringBuilder
    {
        Host = database["Host"] ?? throw new InvalidOperationException("Database:Host is required."),
        Port = database.GetValue("Port", 5432),
        Database = database["Name"] ?? throw new InvalidOperationException("Database:Name is required."),
        Username = database["Username"] ?? throw new InvalidOperationException("Database:Username is required."),
        Password = database["Password"] ?? throw new InvalidOperationException("Database:Password is required.")
    };
    connectionString = connectionSettings.ConnectionString;
    dbHost = connectionSettings.Host;
    usesSqlServer = false;
}

DbProviderFactory providerFactory = usesSqlServer
    ? SqlClientFactory.Instance
    : NpgsqlFactory.Instance;
var connectionFactory = new DatabaseConnectionFactory(
    providerFactory,
    connectionString,
    dbHost,
    usesSqlServer);
builder.Services.AddSingleton(connectionFactory);
builder.Services.AddScoped<IServiceRecordService>(services =>
    new ServiceRecordService(
        connectionFactory,
        services.GetRequiredService<ILogger<ServiceRecordService>>()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Local PostgreSQL is provisioned automatically. Azure SQL schema is installed separately
// with api/sql/azure-service-records.sql so the runtime login needs no DDL permissions.
if (!usesSqlServer)
{
    await using var connection = await connectionFactory.OpenConnectionAsync(CancellationToken.None);
    await using var command = connection.CreateCommand();
    command.CommandText =
        """
        CREATE TABLE IF NOT EXISTS service_records (
            id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            service_name VARCHAR(200) NOT NULL,
            environment VARCHAR(100) NOT NULL,
            status VARCHAR(100) NOT NULL,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """;
    await command.ExecuteNonQueryAsync();
}

app.MapGet("/health", () => TypedResults.Ok(new HealthResponse("healthy")));
app.MapServiceRecords();

await app.RunAsync();

public sealed record HealthResponse(string Status);
