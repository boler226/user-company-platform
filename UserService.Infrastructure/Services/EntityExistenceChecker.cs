using System.Net;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Services;

public class EntityExistenceChecker(HttpClient client) : IEntityExistenceChecker
{
    public async Task<bool> CompanyExistsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync($"api/Companies/GetById/{companyId}", cancellationToken);
        return response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent;
    }
}
