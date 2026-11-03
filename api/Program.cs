using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var configuredConnectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

NpgsqlConnectionStringBuilder connectionSettings;

if (!string.IsNullOrWhiteSpace(configuredConnectionString))
{
    // Azure provides ConnectionStrings__DefaultConnection.
    connectionSettings = new NpgsqlConnectionStringBuilder(configuredConnectionString);
}
else
{
    // Local Compose provides separate Database__* settings.
    var database = builder.Configuration.GetSection("Database");
    connectionSettings = new NpgsqlConnectionStringBuilder
    {
        Host = database["Host"] ?? throw new InvalidOperationException("Database:Host is required."),
        Port = database.GetValue("Port", 5432),
        Database = database["Name"] ?? throw new InvalidOperationException("Database:Name is required."),
        Username = database["Username"] ?? throw new InvalidOperationException("Database:Username is required."),
        Password = database["Password"] ?? throw new InvalidOperationException("Database:Password is required.")
    };
}

var connectionString = connectionSettings.ConnectionString;
var dbHost = connectionSettings.Host;

var dataSource = NpgsqlDataSource.Create(connectionString);
builder.Services.AddSingleton(dataSource);
builder.Services.AddScoped<IServiceRecordService>(services =>
    new ServiceRecordService(
        dataSource,
        services.GetRequiredService<ILogger<ServiceRecordService>>(),
        dbHost));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await using (var connection = await dataSource.OpenConnectionAsync())
await using (var command = new NpgsqlCommand(
    """
    CREATE TABLE IF NOT EXISTS service_records (
        id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
        service_name VARCHAR(200) NOT NULL,
        environment VARCHAR(100) NOT NULL,
        status VARCHAR(100) NOT NULL,
        created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
    );
    """,
    connection))
{
    await command.ExecuteNonQueryAsync();
}

app.MapGet("/health", () => TypedResults.Ok(new HealthResponse("healthy")));
app.MapServiceRecords();

await app.RunAsync();

public sealed record HealthResponse(string Status);
