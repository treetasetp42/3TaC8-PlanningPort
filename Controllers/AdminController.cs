using _3TaC8_PlanningPort.Data;
using _3TaC8_PlanningPort.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace _3TaC8_PlanningPort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Guard helper ───────────────────────────────────────────────────────
        private async Task<bool> IsAdminAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return false;

            var user = await _context.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r!.RolePermissions)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user?.Role?.RolePermissions
                .Any(rp => rp.PermissionKey == "ADMIN_ACCESS") ?? false;
        }

        // ── GET /api/admin/users ───────────────────────────────────────────────
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (!await IsAdminAsync()) return Forbid();

            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.OAuthProviders)
                .OrderBy(u => u.CreatedAt);

            var total = await query.CountAsync();

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.RemoteUser,
                    u.DisplayName,
                    u.Email,
                    u.AvatarUrl,
                    u.CreatedAt,
                    u.BannedUntil,
                    u.BanReason,
                    u.IsActive,
                    RoleId = u.RoleId,
                    RoleName = u.Role != null ? u.Role.Name : "Member",
                    IsGoogleLinked = u.OAuthProviders.Any(o => o.ProviderName == "Google"),
                    IsBanned = u.BannedUntil.HasValue && u.BannedUntil.Value > DateTime.UtcNow
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, users });
        }

        // ── PUT /api/admin/users/{id}/role ─────────────────────────────────────
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> ChangeUserRole(Guid id, [FromBody] ChangeRoleRequest request)
        {
            if (!await IsAdminAsync()) return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            var role = await _context.Roles.FindAsync(request.RoleId);
            if (role == null) return BadRequest("Invalid role.");

            user.RoleId = request.RoleId;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"User role updated to {role.Name}." });
        }

        // ── PUT /api/admin/users/{id}/ban ──────────────────────────────────────
        [HttpPut("users/{id}/ban")]
        public async Task<IActionResult> BanUser(Guid id, [FromBody] BanUserRequest request)
        {
            if (!await IsAdminAsync()) return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            user.BannedUntil = request.BanUntil ?? DateTime.MaxValue;
            user.BanReason = request.Reason;

            _context.UserPenalties.Add(new UserPenalty
            {
                UserId = id,
                AdminId = adminId,
                Action = "Ban",
                Reason = request.Reason,
                Details = $"Banned until {user.BannedUntil:yyyy-MM-dd HH:mm} UTC"
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "User banned.", bannedUntil = user.BannedUntil, reason = user.BanReason });
        }

        // ── PUT /api/admin/users/{id}/unban ────────────────────────────────────
        [HttpPut("users/{id}/unban")]
        public async Task<IActionResult> UnbanUser(Guid id)
        {
            if (!await IsAdminAsync()) return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            user.BannedUntil = null;
            user.BanReason = null;

            _context.UserPenalties.Add(new UserPenalty
            {
                UserId = id,
                AdminId = adminId,
                Action = "Unban",
                Reason = "Administrator manually unbanned the user."
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "User unbanned." });
        }

        // ── PUT /api/admin/users/{id}/status ───────────────────────────────────
        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> SetUserStatus(Guid id, [FromBody] SetStatusRequest request)
        {
            if (!await IsAdminAsync()) return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            user.IsActive = request.IsActive;

            _context.UserPenalties.Add(new UserPenalty
            {
                UserId = id,
                AdminId = adminId,
                Action = request.IsActive ? "Enable" : "Disable",
                Reason = request.Reason
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = $"User account {(request.IsActive ? "enabled" : "disabled")}." });
        }

        // ── PUT /api/admin/users/{id}/profile ──────────────────────────────────
        [HttpPut("users/{id}/profile")]
        public async Task<IActionResult> UpdateUserProfile(Guid id, [FromBody] UpdateUserProfileRequest request)
        {
            if (!await IsAdminAsync()) return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            user.DisplayName = request.DisplayName;
            user.Email = request.Email;
            user.AvatarUrl = request.AvatarUrl;

            await _context.SaveChangesAsync();
            return Ok(new { message = "User profile updated successfully." });
        }

        // ── POST /api/admin/users/{id}/reset-password ──────────────────────────
        [HttpPost("users/{id}/reset-password")]
        public async Task<IActionResult> AdminResetPassword(Guid id, [FromBody] AdminResetPasswordRequest request)
        {
            if (!await IsAdminAsync()) return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            string passwordToSet = request.NewPassword;
            bool isAutoGenerated = string.IsNullOrWhiteSpace(passwordToSet);

            if (isAutoGenerated)
            {
                passwordToSet = GenerateRandomPassword(12);
            }

            user.RemotePassword = BCrypt.Net.BCrypt.HashPassword(passwordToSet);

            _context.UserPenalties.Add(new UserPenalty
            {
                UserId = id,
                AdminId = adminId,
                Action = "PasswordReset",
                Details = isAutoGenerated ? "Auto-generated new password." : "Manually set new password."
            });

            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Password reset successfully.", 
                newPassword = isAutoGenerated ? passwordToSet : null 
            });
        }

        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789"; // Removed look-alikes like 0, O, I, l
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // ── GET /api/admin/roles ───────────────────────────────────────────────
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            if (!await IsAdminAsync()) return Forbid();

            var roles = await _context.Roles
                .Include(r => r.RolePermissions)
                .OrderBy(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Description,
                    r.CreatedAt,
                    Permissions = r.RolePermissions.Select(rp => rp.PermissionKey).ToList()
                })
                .ToListAsync();

            // Also return all available permissions for the toggle UI
            var allPermissions = await _context.Permissions
                .OrderBy(p => p.Module).ThenBy(p => p.Key)
                .Select(p => new { p.Key, p.Description, p.Module })
                .ToListAsync();

            return Ok(new { roles, allPermissions });
        }

        // ── PUT /api/admin/roles/{roleId}/permissions ──────────────────────────
        [HttpPut("roles/{roleId}/permissions")]
        public async Task<IActionResult> UpdateRolePermissions(int roleId, [FromBody] UpdateRolePermissionsRequest request)
        {
            if (!await IsAdminAsync()) return Forbid();

            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null) return NotFound("Role not found.");

            // Remove all current permissions for this role
            _context.RolePermissions.RemoveRange(role.RolePermissions);

            // Add the new set
            foreach (var key in request.PermissionKeys)
            {
                var permExists = await _context.Permissions.AnyAsync(p => p.Key == key);
                if (permExists)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionKey = key
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Permissions updated for role '{role.Name}'." });
        }

        // ── GET /api/admin/roles/list (for role dropdown) ──────────────────────
        [HttpGet("roles/list")]
        public async Task<IActionResult> GetRolesList()
        {
            if (!await IsAdminAsync()) return Forbid();

            var roles = await _context.Roles
                .OrderBy(r => r.Id)
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();

            return Ok(roles);
        }
    }

    // ── Request DTOs ───────────────────────────────────────────────────────────
    public class ChangeRoleRequest
    {
        public int RoleId { get; set; }
    }

    public class BanUserRequest
    {
        public DateTime? BanUntil { get; set; } // null = permanent
        public string? Reason { get; set; }
    }

    public class SetStatusRequest
    {
        public bool IsActive { get; set; }
        public string? Reason { get; set; }
    }

    public class AdminResetPasswordRequest
    {
        public string? NewPassword { get; set; } // if null or empty, system generates one
    }

    public class UpdateUserProfileRequest
    {
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class UpdateRolePermissionsRequest
    {
        public List<string> PermissionKeys { get; set; } = new();
    }
}
