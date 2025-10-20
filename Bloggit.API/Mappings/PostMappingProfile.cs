using AutoMapper;
using Bloggit.API.DTOs;

namespace Bloggit.API.Mappings
{
    /// <summary>
    /// AutoMapper profile for Post entity mappings
    /// This class defines how Post entity maps to/from DTOs
    /// </summary>
    public class PostMappingProfile : Profile
    {
        public PostMappingProfile()
        {
            // Map from Post entity to PostDto
            CreateMap<Post, PostDto>();

            // Map from CreatePostDto to Post entity
            CreateMap<CreatePostDto, Post>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            // Map from UpdatePostDto to Post entity
            CreateMap<UpdatePostDto, Post>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.AuthorId, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
