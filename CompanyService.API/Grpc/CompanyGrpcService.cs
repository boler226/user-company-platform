using CompanyService.Application.Catalog;
using CompanyService.Domain.Exceptions;
using Grpc.Core;
using Npgsql;
using Platform.Grpc.Companies.V1;

namespace CompanyService.API.Grpc;

public sealed class CompanyGrpcService(ICompanyService companies) : CompanyReader.CompanyReaderBase
{
    public override async Task<Company> GetCompany(GetCompanyRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var companyId) || companyId == Guid.Empty)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "A valid company ID is required."));

        try
        {
            var company = await companies.GetAsync(companyId, context.CancellationToken);
            return new Company { Id = company.Id.ToString(), Name = company.Name, Version = company.Version };
        }
        catch (NotFoundException error)
        {
            throw new RpcException(new Status(StatusCode.NotFound, error.Message));
        }
        catch (NpgsqlException error) when (error.IsTransient)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, "Company storage is unavailable."));
        }
    }
}
