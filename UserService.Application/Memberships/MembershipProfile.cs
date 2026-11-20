using AutoMapper;
using UserService.Domain.Memberships;

namespace UserService.Application.Memberships;

public class MembershipProfile : Profile
{
    public MembershipProfile()
    {
        CreateMap<Membership, MembershipDto>();
        CreateMap<MembershipHistory, MembershipHistoryDto>();
    }
}
