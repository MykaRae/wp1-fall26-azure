using System.Data;
using System.Data.Common;
using System.Diagnostics;

public sealed class ServiceRecordService(
    DatabaseConnectionFactory connectionFactory,
    ILogger<ServiceRecordService> logger) : IServiceRecordService
{
    public async Task<ServiceRecordResponse> CreateAsync(
        CreateServiceRecordRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        logger.LogDebug("Renting connection from pool for host {DbHost}", connectionFactory.Host);

        try
        {
            await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = connectionFactory.UsesSqlServer
                ? """
                  INSERT INTO dbo.service_records (service_name, environment, status)
                  OUTPUT INSERTED.id, INSERTED.service_name, INSERTED.environment,
                         INSERTED.status, INSERTED.created_at
                  VALUES (@service_name, @environment, @status);
                  """
                : """
                  INSERT INTO service_records (service_name, environment, status)
                  VALUES (@service_name, @environment, @status)
                  RETURNING id, service_name, environment, status, created_at;
                  """;
            AddParameter(command, "@service_name", request.ServiceName.Trim(), DbType.String);
            AddParameter(command, "@environment", request.Environment.Trim(), DbType.String);
            AddParameter(command, "@status", request.Status.Trim(), DbType.String);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("The database insert returned no service record.");
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
        catch (DbException exception)
        {
            stopwatch.Stop();
            logger.LogError(
                exception,
                "Database operation failed against {DbHost} for service {ServiceName}",
                connectionFactory.Host,
                request.ServiceName);
            throw;
        }
    }

    public async Task<ServiceRecordResponse?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = connectionFactory.UsesSqlServer
            ? """
              SELECT id, service_name, environment, status, created_at
              FROM dbo.service_records
              WHERE id = @id;
              """
            : """
              SELECT id, service_name, environment, status, created_at
              FROM service_records
              WHERE id = @id;
              """;
        AddParameter(command, "@id", id, DbType.Int64);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadResponse(reader) : null;
    }

    private static void AddParameter(DbCommand command, string name, object value, DbType type)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static ServiceRecordResponse ReadResponse(DbDataReader reader) =>
        new(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetFieldValue<DateTimeOffset>(4));
}
