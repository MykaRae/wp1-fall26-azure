var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new HealthResponse("healthy")));

app.Run();

public sealed record HealthResponse(string Status);
