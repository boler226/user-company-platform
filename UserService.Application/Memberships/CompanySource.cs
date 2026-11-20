using System.Net;
using System.Net.Http.Json;
using UserService.Domain.Memberships;

namespace UserService.Application.Memberships;

public interface ICompanySource
{
    Task<DirectoryCompany> GetAsync(Guid id, CancellationToken ct);
}

public sealed class CompanySource(HttpClient client) : ICompanySource
{
    private sealed record CompanyResponse(Guid Id, string Name, long Version);

    public async Task<DirectoryCompany> GetAsync(Guid id, CancellationToken ct)
    {
        using var response = await client.GetAsync($"api/Companies/GetById/{id}", ct);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
        {
            throw new ResourceNotFoundException("Company not found.");
        }

        response.EnsureSuccessStatusCode();
        var company = await response.Content.ReadFromJsonAsync<CompanyResponse>(cancellationToken: ct)
            ?? throw new ResourceNotFoundException("Company not found.");

        return new DirectoryCompany { Id = company.Id, Name = company.Name, SourceVersion = company.Version };
    }
}
