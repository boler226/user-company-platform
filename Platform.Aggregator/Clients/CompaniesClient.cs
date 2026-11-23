using Platform.Aggregator.Models;

namespace Platform.Aggregator.Clients;

public sealed class CompaniesClient(HttpClient http, ILogger<CompaniesClient> logger)
    : ServiceClient(http, logger, "companies-api")
{
    public Task<ServiceResult<CompanyDto>> GetAsync(Guid companyId, CancellationToken ct) =>
        GetAsync<CompanyDto>($"/api/companies/{companyId}", ct);
}
