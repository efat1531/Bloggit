using Bloggit.Data.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Bloggit.API.Authorization
{
    /// <summary>
    /// Authorization handler that checks if the user is an Admin or the owner of a Post resource.
    /// </summary>
    public class PostOwnershipAuthorizationHandler : AuthorizationHandler<ResourceOwnershipRequirement, Post>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ResourceOwnershipRequirement requirement,
            Post resource)
        {
            // Check if user is authenticated
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                return Task.CompletedTask;
            }

            // Check if user is an Admin
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Check if user is the author of the post
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null && resource.AuthorId == userId)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // If neither Admin nor Author, the requirement is not met
            return Task.CompletedTask;
        }
    }
}
