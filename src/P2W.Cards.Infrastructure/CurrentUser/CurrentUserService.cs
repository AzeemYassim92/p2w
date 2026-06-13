using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Infrastructure.CurrentUser;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserService> logger) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            var raw = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub");
            if (Guid.TryParse(raw, out var id))
            {
                return id;
            }

            var fallback = Guid.Parse("00000000-0000-0000-0000-000000000001");
            logger.LogWarning("Using development fallback user id {UserId}. Configure JWT auth to remove this fallback.", fallback);
            return fallback;
        }
    }

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
}
