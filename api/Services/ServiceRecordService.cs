using System.Diagnostics;
using Npgsql;

public sealed class ServiceRecordService(
    NpgsqlDataSource dataSource,
    ILogger<ServiceRecordService> logger,
    string dbHost) : IServiceRecordService
{
    public async Task<ServiceRecordResponse> CreateAsync(
        CreateServiceRecordRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        logger.LogDebug("Renting connection from pool for host {DbHost}", dbHost);

        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO service_records (service_name, environment, status)
                VALUES (@service_name, @environment, @status)
                RETURNING id, service_name, environment, status, created_at;
                """,
                connection);
            command.Parameters.AddWithValue("service_name", request.ServiceName.Trim());
            command.Parameters.AddWithValue("environment", request.Environment.Trim());
            command.Parameters.AddWithValue("status", request.Status.Trim());

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            var record = ReadResponse(reader);

            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 150)
            {
                logger.LogWarning(
                    "Slow query detected on {Endpoint}: took {ElapsedMs}ms",
                    "/api/services",
                    stopwatch.ElapsedMilliseconds);
            }

            logger.LogInformation(
                "Successfully created service record {ServiceName} with ID {ServiceId} in {ElapsedMs}ms",
                record.ServiceName,
                record.Id,
                stopwatch.ElapsedMilliseconds);

            return record;
        }
        catch (NpgsqlException exception)
        {
            stopwatch.Stop();
            logger.LogError(
                exception,
                "Database operation failed against {DbHost} for service {ServiceName}",
                dbHost,
                request.ServiceName);
            throw;
        }
    }

    public async Task<ServiceRecordResponse?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, service_name, environment, status, created_at
            FROM service_records
            WHERE id = @id;
            """,
            connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadResponse(reader) : null;
    }

    private static ServiceRecordResponse ReadResponse(NpgsqlDataReader reader) =>
        new(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetFieldValue<DateTimeOffset>(4));
}
