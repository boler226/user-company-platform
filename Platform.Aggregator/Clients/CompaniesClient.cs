using Platform.Aggregator.Models;
using Platform.Grpc.Companies.V1;

namespace Platform.Aggregator.Clients;

public sealed class CompaniesClient(CompanyReader.CompanyReaderClient client, ILogger<CompaniesClient> logger)
    : ServiceClient(logger, "companies-api")
{
    public Task<ServiceResult<CompanyDto>> GetAsync(Guid id, CancellationToken ct) =>
        CallAsync(async (deadline, token) =>
        {
            var item = await client.GetCompanyAsync(new GetCompanyRequest { Id = id.ToString() },
                deadline: deadline, cancellationToken: token);
            return new CompanyDto(Guid.Parse(item.Id), item.Name, item.Version);
        }, ct);
}
