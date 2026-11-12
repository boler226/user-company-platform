using CompanyService.Application.Behaviors;
using MediatR;

namespace CompanyService.Application.Commands.UpdateCompany {
    public record UpdateCompanyCommand(Guid Id, string Name, string Email, string Address) : IRequest, ICachedQuery
    {
        public string CacheKey => $"company:{Id}";
    }
}
