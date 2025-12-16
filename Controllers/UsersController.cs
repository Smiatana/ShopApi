using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ShopContext _context;

    public UsersController(ShopContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.Images)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound();

        return user;
    }


    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, User user)
    {
        if (id != user.Id)
            return BadRequest();

        var loggedInEmail = User.Identity?.Name;
        var loggedInRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var existingUser = await _context.Users.FindAsync(id);
        if (existingUser == null)
        {
            return NotFound();
        }

        if (loggedInRole != "Admin" && existingUser.Email != loggedInEmail)
        {
            return Forbid();
        }

        existingUser.Name = user.Name;
        existingUser.ProfileInfo = user.ProfileInfo;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Users.Any(e => e.Id == id))
                return NotFound();
            else
                throw;
        }
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<User>> GetMe()
    {
        var email = User.Identity?.Name;

        var user = await _context.Users
            .Include(u => u.Images)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return NotFound();

        return user;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserAdminDto>>> GetAllUsers()
    {
        var users = await _context.Users
            .Select(u => new UserAdminDto
            {
                Id = u.Id,
                Email = u.Email,
                Name = u.Name,
                Role = u.Role
            })
            .ToListAsync();
        return Ok(users);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/role")]
    public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (request.NewRole != "User" && request.NewRole != "Admin")
            return BadRequest("Role must be 'User' or 'Admin'");

        user.Role = request.NewRole;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.Images)
            .Include(u => u.Carts)
            .Include(u => u.Orders)
            .Include(u => u.Reviews)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }


}
