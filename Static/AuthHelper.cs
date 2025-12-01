using System.Security.Claims;

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

    public static int GetUserId(ClaimsPrincipal user)
    {
        return int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
