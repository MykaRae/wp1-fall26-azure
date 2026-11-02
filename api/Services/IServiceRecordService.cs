public interface IServiceRecordService
{
    Task<ServiceRecordResponse> CreateAsync(
        CreateServiceRecordRequest request,
        CancellationToken cancellationToken);

    Task<ServiceRecordResponse?> GetByIdAsync(long id, CancellationToken cancellationToken);
}
