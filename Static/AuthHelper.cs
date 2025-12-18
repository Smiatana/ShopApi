using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

public static class AuthHelper
{
    public static bool IsOwnerOrAdmin(ClaimsPrincipal user, int ownerId)
    {
        var role = user.FindFirstValue(ClaimTypes.Role);
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim))
            return false;

        var userId = int.Parse(userIdClaim);

        if (role == "Admin")
            return true;
            
        return userId == ownerId;
    }

    public static int GetUserId(ClaimsPrincipal user, ShopContext context)
    {
        var email = user.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(email))
            throw new UnauthorizedAccessException("Email claim not found");

        var dbUser = context.Users
            .AsNoTracking()
            .FirstOrDefault(u => u.Email == email);

        if (dbUser == null)
            throw new UnauthorizedAccessException("User not found");

        return dbUser.Id;
    }
}
