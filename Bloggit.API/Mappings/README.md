# Mappings Folder

This folder contains all AutoMapper profile configurations for the Bloggit API project.

## Purpose

AutoMapper profiles define how entities are mapped to/from Data Transfer Objects (DTOs). Each profile is a class that inherits from `Profile` and configures mappings in its constructor.

## Current Profiles

### PostMappingProfile.cs

Defines mappings for the Post entity:

- `Post` → `PostDto` (read operations)
- `CreatePostDto` → `Post` (create operations)
- `UpdatePostDto` → `Post` (update operations)

## How to Add New Profiles

### 1. Create a new file: `{EntityName}MappingProfile.cs`

```csharp
using AutoMapper;
using Bloggit.API.DTOs;

namespace Bloggit.API.Mappings
{
    public class CommentMappingProfile : Profile
    {
        public CommentMappingProfile()
        {
            // Define your mappings here
            CreateMap<Comment, CommentDto>();
            CreateMap<CreateCommentDto, Comment>();
            CreateMap<UpdateCommentDto, Comment>();
        }
    }
}
```

### 2. That's it!

AutoMapper automatically discovers and registers all classes that inherit from `Profile` in this assembly. No additional configuration is needed.

## Best Practices

1. **One profile per entity** - Keep related mappings together
2. **Name consistently** - Use `{EntityName}MappingProfile` pattern
3. **Document complex mappings** - Add comments for non-obvious transformations
4. **Handle timestamps** - Set `CreatedAt` and `UpdatedAt` automatically
5. **Ignore read-only properties** - Use `.ForMember(dest => dest.Id, opt => opt.Ignore())`

## Common Patterns

### Basic Mapping

```csharp
CreateMap<Source, Destination>();
```

### Ignore Property

```csharp
CreateMap<Source, Destination>()
    .ForMember(dest => dest.PropertyName, opt => opt.Ignore());
```

### Set Default Value

```csharp
CreateMap<CreateDto, Entity>()
    .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
```

### Conditional Mapping (for updates)

```csharp
CreateMap<UpdateDto, Entity>()
    .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
```

## Documentation

For detailed guides, see:

- **Full Documentation**: `h:\Dot Net Project\AUTOMAPPER_SETUP.md`
- **Quick Reference**: `h:\Dot Net Project\AUTOMAPPER_QUICK_REFERENCE.md`
- **Architecture**: `h:\Dot Net Project\AUTOMAPPER_ARCHITECTURE.md`

---

**Location**: `h:\Dot Net Project\Bloggit.API\Mappings\`
