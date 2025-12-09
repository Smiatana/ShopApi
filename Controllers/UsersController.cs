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
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

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

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

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

}
