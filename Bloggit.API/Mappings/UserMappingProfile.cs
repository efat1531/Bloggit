using AutoMapper;
using Bloggit.Data.Models;
using Bloggit.Models.User;

namespace Bloggit.API.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // Response mappings
        CreateMap<ApplicationUser, UserResponse>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.UserName));

        CreateMap<ApplicationUser, UserWithRolesResponse>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.UserName))
            .ForMember(dest => dest.Roles, opt => opt.Ignore()); // Roles populated in controller

        // Request mappings
        CreateMap<RegisterRequest, ApplicationUser>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
            .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
            .ForMember(dest => dest.EmailConfirmed, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
            .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore())
            .ForMember(dest => dest.PhoneNumberConfirmed, opt => opt.Ignore())
            .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
            .ForMember(dest => dest.Posts, opt => opt.Ignore())
            .ForMember(dest => dest.Comments, opt => opt.Ignore());

        CreateMap<UpdateUserProfileRequest, ApplicationUser>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
