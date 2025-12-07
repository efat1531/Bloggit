using AutoMapper;
using Bloggit.API.DTOs;
using Bloggit.Data.Models;

namespace Bloggit.API.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // ApplicationUser to UserDto
        CreateMap<ApplicationUser, UserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.UserName));

        // RegisterDto to ApplicationUser
        CreateMap<RegisterDto, ApplicationUser>()
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

        // UpdateUserProfileDto to ApplicationUser
        CreateMap<UpdateUserProfileDto, ApplicationUser>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
