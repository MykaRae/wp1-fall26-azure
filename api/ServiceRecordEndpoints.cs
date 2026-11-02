using Microsoft.AspNetCore.Http.HttpResults;

public static class ServiceRecordEndpoints
{
    public static WebApplication MapServiceRecords(this WebApplication app)
    {
        app.MapPost("/api/services", Create)
            .WithName("CreateServiceRecord")
            .WithSummary("Create a service record")
            .Produces<ServiceRecordResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        app.MapGet("/api/services/{id:long}", GetById)
            .WithName("GetServiceRecordById")
            .WithSummary("Get a service record by ID")
            .Produces<ServiceRecordResponse>()
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<Created<ServiceRecordResponse>, ValidationProblem>> Create(
        CreateServiceRecordRequest request,
        IServiceRecordService service,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.ServiceName))
            errors[nameof(request.ServiceName)] = ["Service name is required."];
        else if (request.ServiceName.Length > 200)
            errors[nameof(request.ServiceName)] = ["Service name must be 200 characters or fewer."];
        if (string.IsNullOrWhiteSpace(request.Environment))
            errors[nameof(request.Environment)] = ["Environment is required."];
        else if (request.Environment.Length > 100)
            errors[nameof(request.Environment)] = ["Environment must be 100 characters or fewer."];
        if (string.IsNullOrWhiteSpace(request.Status))
            errors[nameof(request.Status)] = ["Status is required."];
        else if (request.Status.Length > 100)
            errors[nameof(request.Status)] = ["Status must be 100 characters or fewer."];

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var record = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/services/{record.Id}", record);
    }

    private static async Task<Results<Ok<ServiceRecordResponse>, NotFound>> GetById(
        long id,
        IServiceRecordService service,
        CancellationToken cancellationToken)
    {
        var record = await service.GetByIdAsync(id, cancellationToken);
        return record is null ? TypedResults.NotFound() : TypedResults.Ok(record);
    }
}
