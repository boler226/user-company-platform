using AutoMapper;
using CompanyService.Application.DTOs;
using CompanyService.Domain.Entities;
using CompanyService.Domain.Exceptions;
using CompanyService.Domain.Specifications;
using CompanyService.Infrastructure.UnitOfWork.Interfaces;
using FluentValidation;

namespace CompanyService.Application.Catalog;

public sealed class CompanyCatalogService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IValidator<CompanyWriteRequest> companyValidator,
    IValidator<CompanyQuery> queryValidator,
    IValidator<DepartmentRequest> departmentValidator) : ICompanyService
{
    public async Task<PagedResult<CompanyDto>> SearchAsync(CompanyQuery query, CancellationToken ct)
    {
        await queryValidator.ValidateAndThrowAsync(query, ct);
        var specification = new CompanySearchSpecification(
            query.Search, query.TagId, query.SortBy, query.SortDir == "desc", query.Page, query.PageSize);
        var (items, total) = await unitOfWork.Companies.SearchAsync(specification, ct);
        return new PagedResult<CompanyDto>(mapper.Map<List<CompanyDto>>(items), total, query.Page, query.PageSize);
    }

    public async Task<CompanyView> GetAsync(Guid id, CancellationToken ct) =>
        mapper.Map<CompanyView>(await unitOfWork.Companies.GetDetailsAsync(id, ct)
            ?? throw new NotFoundException("Company not found."));

    public async Task<CompanyView> CreateAsync(CompanyWriteRequest input, CancellationToken ct)
    {
        await companyValidator.ValidateAndThrowAsync(input, ct);
        await EnsureEmailAvailableAsync(input.Email, null, ct);
        var company = new Company(input.Name, input.Email, input.Address);
        await unitOfWork.Companies.AddAsync(company, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return mapper.Map<CompanyView>(company);
    }

    public async Task UpdateAsync(Guid id, CompanyWriteRequest input, CancellationToken ct)
    {
        await companyValidator.ValidateAndThrowAsync(input, ct);
        var company = await FindAsync(id, ct);
        if (company.Version != input.Version)
        {
            throw new ConflictException("The company version has changed.");
        }
        await EnsureEmailAvailableAsync(input.Email, id, ct);
        company.Rename(input.Name, input.Email, input.Address);
        unitOfWork.Companies.Update(company);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var company = await FindAsync(id, ct);
        company.Archive();
        unitOfWork.Companies.Update(company);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<List<DepartmentDto>> DepartmentsAsync(Guid id, CancellationToken ct)
    {
        var company = await FindAsync(id, ct);
        return mapper.Map<List<DepartmentDto>>(await unitOfWork.Departments.LoadForCompanyAsync(company, ct));
    }

    public async Task<DepartmentDto> GetDepartmentAsync(Guid companyId, Guid departmentId, CancellationToken ct)
    {
        await FindAsync(companyId, ct);
        var department = await unitOfWork.Departments.GetByIdAsync(departmentId, ct);
        if (department is null || department.CompanyId != companyId)
        {
            throw new NotFoundException("Department not found.");
        }

        return mapper.Map<DepartmentDto>(department);
    }

    public async Task<DepartmentDto> AddDepartmentAsync(Guid id, DepartmentRequest input, CancellationToken ct)
    {
        await departmentValidator.ValidateAndThrowAsync(input, ct);
        await FindAsync(id, ct);
        if (await unitOfWork.Departments.NameExistsAsync(id, input.Name.Trim(), ct))
        {
            throw new ConflictException("Department name already exists in this company.");
        }

        var department = new Department(id, input.Name, input.Budget);
        await unitOfWork.Departments.AddAsync(department, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return mapper.Map<DepartmentDto>(department);
    }

    public async Task<List<TagDto>> TagsAsync(CancellationToken ct) =>
        mapper.Map<List<TagDto>>(await unitOfWork.Tags.GetAllAsync(ct));

    public async Task<List<CompanyTagDto>> CompanyTagsAsync(Guid id, CancellationToken ct)
    {
        await FindAsync(id, ct);
        return mapper.Map<List<CompanyTagDto>>(await unitOfWork.Tags.GetCompanyTagsAsync(id, ct));
    }

    public async Task AddTagAsync(Guid companyId, Guid tagId, CancellationToken ct)
    {
        await FindAsync(companyId, ct);
        if (await unitOfWork.Tags.GetByIdAsync(tagId, ct) is null)
        {
            throw new NotFoundException("Tag not found.");
        }
        if (await unitOfWork.Tags.LinkExistsAsync(companyId, tagId, ct))
        {
            throw new ConflictException("Tag is already assigned.");
        }
        await unitOfWork.Tags.AddLinkAsync(new CompanyTag(companyId, tagId), ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Company> FindAsync(Guid id, CancellationToken ct) =>
        await unitOfWork.Companies.GetByIdAsync(id, ct) ?? throw new NotFoundException("Company not found.");

    private async Task EnsureEmailAvailableAsync(string email, Guid? currentId, CancellationToken ct)
    {
        var existing = await unitOfWork.Companies.GetByEmailAsync(email.Trim().ToLowerInvariant(), ct);
        if (existing is not null && existing.Id != currentId)
        {
            throw new ConflictException("Company email already exists.");
        }
    }
}
