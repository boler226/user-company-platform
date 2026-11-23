using AutoMapper;
using CompanyService.Application.DTOs;
using CompanyService.Application.Catalog;
using CompanyService.Domain.Interfaces;
using CompanyService.Domain.Entities;

namespace CompanyService.Application.Mappings;

public class CompanyMappingProfile : Profile
{
    public CompanyMappingProfile()
    {
        CreateMap<Company, CompanyDto>();
        CreateMap<Company, CompanyView>()
            .IncludeBase<Company, CompanyDto>()
            .ForMember(destination => destination.Tags,
                options => options.MapFrom(source => source.CompanyTags.Select(link => link.Tag)));
        CreateMap<Department, DepartmentDto>();
        CreateMap<Tag, TagDto>();
        CreateMap<TaggedCompany, CompanyTagDto>();
    }
}
