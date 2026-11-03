using System.Data.Common;

public sealed class DatabaseConnectionFactory(
    DbProviderFactory providerFactory,
    string connectionString,
    string host,
    bool usesSqlServer)
{
    public string Host { get; } = host;
    public bool UsesSqlServer { get; } = usesSqlServer;

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = providerFactory.CreateConnection()
            ?? throw new InvalidOperationException("The database provider could not create a connection.");
        connection.ConnectionString = connectionString;

        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
