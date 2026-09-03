using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Identity;
using TmsApi.Domain.Entities;

namespace TmsApi.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<TmsUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(UserManager<TmsUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public record UserDto(string Id, string Email, string FirstName, string LastName, string Role, bool IsActive);
    public record CreateUserRequest(string Email, string Password, string FirstName, string LastName, string Role);
    public record UpdateUserRequest(string FirstName, string LastName, string Role, bool IsActive);

    /// <summary>
    /// Fetch all users with optional role filtering and text search.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? role, [FromQuery] string? search)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.Email!.ToLower().Contains(term) ||
                                     u.FirstName.ToLower().Contains(term) ||
                                     u.LastName.ToLower().Contains(term));
        }

        var users = await query.ToListAsync();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "Student";

            if (string.IsNullOrEmpty(role) || userRole.Equals(role, System.StringComparison.OrdinalIgnoreCase))
            {
                userDtos.Add(new UserDto(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.FirstName,
                    user.LastName,
                    userRole,
                    !user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= System.DateTimeOffset.UtcNow
                ));
            }
        }

        return Ok(userDtos);
    }

    /// <summary>
    /// Create a new user account with role assignment.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
        {
            return Conflict(new { Message = "A user with this email already exists." });
        }

        var user = new TmsUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            await _roleManager.CreateAsync(new IdentityRole(request.Role));
        }

        await _userManager.AddToRoleAsync(user, request.Role);
        return Created(string.Empty, new UserDto(user.Id, user.Email, user.FirstName, user.LastName, request.Role, true));
    }

    /// <summary>
    /// Update user profile details and role.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;

        // Manage user status (Lockout)
        if (!request.IsActive)
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, System.DateTimeOffset.MaxValue);
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
        }

        await _userManager.UpdateAsync(user);

        // Update role allocation
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        
        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            await _roleManager.CreateAsync(new IdentityRole(request.Role));
        }
        await _userManager.AddToRoleAsync(user, request.Role);

        return Ok(new UserDto(user.Id, user.Email!, user.FirstName, user.LastName, request.Role, request.IsActive));
    }

    /// <summary>
    /// Delete user account.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        await _userManager.DeleteAsync(user);
        return NoContent();
    }
}