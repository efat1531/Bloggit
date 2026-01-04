using Microsoft.AspNetCore.Authorization;

namespace Bloggit.API.Authorization
{
    /// <summary>
    /// Authorization requirement for resource ownership.
    /// Requires that the user is either an Admin or the owner of the resource.
    /// </summary>
    public class ResourceOwnershipRequirement : IAuthorizationRequirement
    {
        // This is a marker class - no properties needed
        // The actual resource is passed during authorization
    }
}
