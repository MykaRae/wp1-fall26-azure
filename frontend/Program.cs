var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8081";
if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri)
    || (apiUri.Scheme != Uri.UriSchemeHttp && apiUri.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException("ApiBaseUrl must be an absolute HTTP or HTTPS URL.");
}

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", async (
    IHttpClientFactory clientFactory,
    CancellationToken cancellationToken) =>
{
    using var client = clientFactory.CreateClient();
    using var response = await client.GetAsync(
        new Uri($"{apiBaseUrl.TrimEnd('/')}/health"),
        cancellationToken);
    var body = await response.Content.ReadAsStringAsync(cancellationToken);
    var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

    return Results.Content(body, contentType, statusCode: (int)response.StatusCode);
});

app.Run();
