using _3TaC8_PlanningPort.Data;
using _3TaC8_PlanningPort.DTOs;
using _3TaC8_PlanningPort.Entities;
using _3TaC8_PlanningPort.Services;
using BCrypt.Net;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace _3TaC8_PlanningPort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserController> _logger;

        public UserController(ApplicationDbContext context, IConfiguration config, IWebHostEnvironment environment, IEmailService emailService, ILogger<UserController> logger)
        {
            _context = context;
            _config = config;
            _environment = environment;
            _emailService = emailService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        [EnableRateLimiting("register")]
        public async Task<ActionResult> Register(CreateUserRequest request)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.RemoteUser == request.RemoteUser))
                {
                    return BadRequest("Username already exists.");
                }

                if (!string.IsNullOrEmpty(request.Email) && await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest("Email already exists.");
                }

                var memberRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Member");
                if (memberRole == null)
                    return StatusCode(503, "Registration is temporarily unavailable.");

                var newUser = new User
                {
                    RemoteUser = request.RemoteUser,
                    RemotePassword = BCrypt.Net.BCrypt.HashPassword(request.RemotePassword),
                    Email = request.Email,
                    DisplayName = request.RemoteUser,
                    RoleId = memberRole.Id
                };
                _context.Users.Add(newUser);

                // บันทึก Log การสมัครสมาชิกใหม่ [cite: 2026-04-02]
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                _context.UserLogs.Add(new UserLog { UserId = newUser.Id, Action = "User Registered", IPAddress = ip });

                await _context.SaveChangesAsync();

                // Create default "Main Portfolio" to ensure new users have a workspace
                var defaultPortfolio = new Portfolio
                {
                    UserId = newUser.Id,
                    Name = "Main Portfolio",
                    Description = "Your initial portfolio profile",
                    ColorCode = "#6C5DD3"
                };
                _context.Portfolios.Add(defaultPortfolio);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed.");
                return StatusCode(500, "Registration could not be completed.");
            }
        }
        [AllowAnonymous]
        [HttpPost("login")] 
        [EnableRateLimiting("auth")]
        public async Task<ActionResult> Login(CreateUserRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RemoteUser == request.RemoteUser);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.RemotePassword, user.RemotePassword))
            {
                if (user != null)
                {
                    var recentlyLogged = await _context.UserLogs.AnyAsync(log =>
                        log.UserId == user.Id && log.Action == "Login Failed" && log.IPAddress == ip &&
                        log.Timestamp > DateTime.UtcNow.AddMinutes(-5));
                    if (!recentlyLogged)
                    {
                        _context.UserLogs.Add(new UserLog { UserId = user.Id, Action = "Login Failed", IPAddress = ip });
                        await _context.SaveChangesAsync();
                    }
                }
                return Unauthorized("Invalid username or password");
            }

            // Check if user is disabled
            if (!user.IsActive)
            {
                return StatusCode(403, "Your account has been disabled by an administrator.");
            }

            // Check if user is banned
            if (user.BannedUntil.HasValue && user.BannedUntil.Value > DateTime.UtcNow)
            {
                var reasonText = string.IsNullOrWhiteSpace(user.BanReason) ? "No reason specified." : user.BanReason;
                return StatusCode(403, $"Your account is banned until {user.BannedUntil:yyyy-MM-dd HH:mm} UTC. Reason: {reasonText}");
            }

            // Deletion countdown check — lazy evaluation at login only (low-load design for Azure Free Tier)
            if (user.DeleteRequestedAt.HasValue)
            {
                if ((DateTime.UtcNow - user.DeleteRequestedAt.Value).TotalDays >= 14)
                {
                    // Time is up — destroy the account and block login
                    await DestroyAccountData(user);
                    return StatusCode(403, "This account has been permanently deleted.");
                }
                // Still within the grace period — allow login but include a warning
            }

            // 1. สร้าง JWT Token [cite: 2026-04-01]
            var token = GenerateJwtToken(user);
            var refreshToken = await GenerateAndSaveRefreshToken(user);

            // Ban check
            if (user.BannedUntil.HasValue && user.BannedUntil.Value > DateTime.UtcNow)
            {
                return StatusCode(403, new { message = $"Your account is banned until {user.BannedUntil.Value:yyyy-MM-dd HH:mm} UTC." });
            }

            var permissions = await GetUserPermissions(user.Id);

            return Ok(new
            {
                accessToken = token,
                refreshToken,
                userId = user.Id,
                expiresIn = 60,
                deleteRequestedAt = user.DeleteRequestedAt,
                permissions
            });
        }

        [AllowAnonymous]
        [HttpPost("google-login")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult> GoogleLogin(GoogleLoginRequest request)
        {
            try
            {
                // 1. Verify Google ID Token
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["web:client_id"] }
                });

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                // 2. Check if this Google account is already linked
                var userOAuth = await _context.UserOAuths
                    .Include(uo => uo.User)
                    .FirstOrDefaultAsync(uo => uo.ProviderName == "Google" && uo.ProviderKey == payload.Subject);

                User? user = userOAuth?.User;

                if (user == null)
                {
                    // 3. Search for existing user by Email if not found by ProviderKey
                    user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

                    if (user != null)
                    {
                        // User exists but isn't linked yet. We MUST NOT auto-link. 
                        // Instead, return a special status instructing the frontend to ask for confirmation.
                        return StatusCode(409, new { 
                            requiresLinking = true, 
                            email = payload.Email
                        });
                    }
                    else
                    {
                        var memberRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Member");
                        if (memberRole == null)
                            return StatusCode(503, "Google registration is temporarily unavailable.");

                        // 4. Create new user if they don't exist at all
                        user = new User
                        {
                            RemoteUser = payload.Email, // Use email as default username
                            Email = payload.Email,
                            DisplayName = payload.Name,
                            AvatarUrl = payload.Picture,
                            RemotePassword = null, // OAuth users have no local password initially
                            RoleId = memberRole.Id
                        };
                        _context.Users.Add(user);
                        _context.UserLogs.Add(new UserLog { UserId = user.Id, Action = "Google User Registered", IPAddress = ip });
                        
                        await _context.SaveChangesAsync(); // Save to generate ID
                        
                        // Create default "Main Portfolio" for the new OAuth user
                        var defaultPortfolio = new Portfolio
                        {
                            UserId = user.Id,
                            Name = "Main Portfolio",
                            Description = "Your initial portfolio profile",
                            ColorCode = "#6C5DD3"
                        };
                        _context.Portfolios.Add(defaultPortfolio);
                        await _context.SaveChangesAsync();

                        // 5. Link Google Account
                        _context.UserOAuths.Add(new UserOAuth
                        {
                            UserId = user.Id,
                            ProviderName = "Google",
                            ProviderKey = payload.Subject
                        });
                    }
                }

                // Check if user is disabled
                if (!user.IsActive)
                {
                    return StatusCode(403, "Your account has been disabled by an administrator.");
                }

                // Ban check
                if (user.BannedUntil.HasValue && user.BannedUntil.Value > DateTime.UtcNow)
                {
                    var reasonText = string.IsNullOrWhiteSpace(user.BanReason) ? "No reason specified." : user.BanReason;
                    return StatusCode(403, $"Your account is banned until {user.BannedUntil.Value:yyyy-MM-dd HH:mm} UTC. Reason: {reasonText}");
                }

                // 6. Generate JWT and Return only after account-state checks pass.
                var token = GenerateJwtToken(user);
                var refreshToken = await GenerateAndSaveRefreshToken(user);
                var permissions = await GetUserPermissions(user.Id);

                return Ok(new
                {
                    accessToken = token,
                    refreshToken,
                    userId = user.Id,
                    expiresIn = 60,
                    permissions
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google login failed.");
                return BadRequest("Invalid Google token.");
            }
        }

        [AllowAnonymous]
        [HttpPost("confirm-google-link")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult> ConfirmGoogleLink(GoogleLoginRequest request)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["web:client_id"] }
                });

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);
                if (user == null) return NotFound("User not found.");
                if (!user.IsActive || (user.BannedUntil.HasValue && user.BannedUntil > DateTime.UtcNow))
                    return StatusCode(403, "This account is not permitted to sign in.");

                var providerAlreadyLinked = await _context.UserOAuths.AnyAsync(uo =>
                    uo.ProviderName == "Google" && (uo.ProviderKey == payload.Subject || uo.UserId == user.Id));
                if (providerAlreadyLinked) return Conflict("A Google account is already linked.");

                // Link them
                _context.UserOAuths.Add(new UserOAuth
                {
                    UserId = user.Id,
                    ProviderName = "Google",
                    ProviderKey = payload.Subject
                });

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                _context.UserLogs.Add(new UserLog { UserId = user.Id, Action = "Google Account Linked (Consent)", IPAddress = ip });
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);
                var refreshToken = await GenerateAndSaveRefreshToken(user);
                var linkPerms = await GetUserPermissions(user.Id);
                return Ok(new { accessToken = token, refreshToken, userId = user.Id, expiresIn = 60, permissions = linkPerms });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google account confirmation failed.");
                return BadRequest("Failed to link account.");
            }
        }

        [HttpPost("link-google")]
        public async Task<ActionResult> LinkGoogleFromSettings(LinkGoogleRequest request)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

                var user = await _context.Users.FindAsync(userId);
                if (user == null) return NotFound("User not found.");

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["web:client_id"] }
                });

                // Check if this provider key is already used by someone else
                var existingLink = await _context.UserOAuths.FirstOrDefaultAsync(uo => uo.ProviderName == "Google" && uo.ProviderKey == payload.Subject);
                if (existingLink != null)
                {
                    if (existingLink.UserId == user.Id) return Ok(new { message = "Account is already linked." });
                    return BadRequest("This Google Profile is linked to another account.");
                }

                _context.UserOAuths.Add(new UserOAuth
                {
                    UserId = user.Id,
                    ProviderName = "Google",
                    ProviderKey = payload.Subject
                });

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                _context.UserLogs.Add(new UserLog { UserId = user.Id, Action = "Google Account Linked (Settings)", IPAddress = ip });
                
                await _context.SaveChangesAsync();
                return Ok(new { message = "Google account linked successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google account linking failed.");
                return BadRequest("Failed to link account.");
            }
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            await _context.Entry(user).Reference(u => u.Role).LoadAsync();
            var permissions = await GetUserPermissions(user.Id);

            return Ok(new
            {
                userId = user.Id,
                username = user.RemoteUser,
                email = user.Email,
                displayName = user.DisplayName,
                avatarUrl = user.AvatarUrl,
                roleId = user.RoleId,
                roleName = user.Role?.Name ?? "Member",
                hasPassword = !string.IsNullOrEmpty(user.RemotePassword),
                isGoogleLinked = await _context.UserOAuths.AnyAsync(uo => uo.UserId == user.Id && uo.ProviderName == "Google"),
                permissions
            });
        }

        [HttpPut("profile")]
        [EnableRateLimiting("upload")]
        [RequestSizeLimit(2_000_000)]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileRequest request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (request.DisplayName != null) user.DisplayName = request.DisplayName;

            if (!string.IsNullOrEmpty(request.RemoteUser) && request.RemoteUser != user.RemoteUser)
            {
                // Only allow username change if it's currently set as the email [cite: 2026-04-11]
                if (user.RemoteUser != user.Email)
                {
                    return BadRequest("Username cannot be changed once set.");
                }

                if (await _context.Users.AnyAsync(u => u.RemoteUser == request.RemoteUser))
                {
                    return BadRequest("Username already exists.");
                }
                user.RemoteUser = request.RemoteUser;
            }
            
            // Handle Email Update [cite: 2026-04-11]
            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                // Check if user is linked to Google OAuth
                bool isGoogleLinked = await _context.UserOAuths.AnyAsync(uo => uo.UserId == user.Id && uo.ProviderName == "Google");
                if (isGoogleLinked)
                {
                    // Prevent email change for OAuth users
                    // return BadRequest("Email cannot be changed while linked to Google OAuth.");
                }
                else
                {
                    // Check if the new email already exists in the system
                    if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                    {
                        return BadRequest("Email already exists.");
                    }
                    user.Email = request.Email;
                }
            }

            // Handle Image Upload or Deletion
            if (request.DeleteCurrentAvatar)
            {
                DeleteLocalAvatarFile(user.AvatarUrl);
                user.AvatarUrl = null;
            }
            else if (request.AvatarFile != null)
            {
                if (!await IsSupportedAvatarAsync(request.AvatarFile))
                    return BadRequest("Avatar must be a JPG, PNG, or WEBP image no larger than 2 MB.");

                // Delete old local file if exists
                DeleteLocalAvatarFile(user.AvatarUrl);

                // Save new file
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid() + Path.GetExtension(request.AvatarFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.AvatarFile.CopyToAsync(stream);
                }

                // Update URL to local path
                user.AvatarUrl = $"/uploads/avatars/{fileName}";
            }
            else if (request.AvatarUrl != null)
            {
                // If they manually set a URL (like from Google or external), keep it
                user.AvatarUrl = request.AvatarUrl;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profile updated successfully", avatarUrl = user.AvatarUrl });
        }

        [HttpPost("change-password")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            bool hasPassword = !string.IsNullOrEmpty(user.RemotePassword);

            if (hasPassword)
            {
                if (string.IsNullOrEmpty(request.CurrentPassword) || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.RemotePassword))
                {
                    return BadRequest("Current password is incorrect.");
                }
            }

            if (request.NewPassword != request.ConfirmNewPassword)
            {
                return BadRequest("New passwords do not match.");
            }

            user.RemotePassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            var activeTokens = await _context.RefreshTokens.Where(rt => rt.UserId == user.Id && rt.Revoked == null).ToListAsync();
            foreach (var token in activeTokens) token.Revoked = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password updated successfully" });
        }

        [HttpPost("unlink-google")]
        public async Task<ActionResult> UnlinkGoogle()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            // Feature 1: Lockout Prevention — must have a local password before unlinking OAuth
            if (string.IsNullOrEmpty(user.RemotePassword))
            {
                return BadRequest("You must set a local password before unlinking your Google account to avoid being locked out.");
            }

            // Feature 3: Quota — find specifically the Google provider for this user
            var googleLink = await _context.UserOAuths
                .FirstOrDefaultAsync(uo => uo.UserId == userId && uo.ProviderName == "Google");

            if (googleLink == null)
            {
                return BadRequest("No Google account is currently linked.");
            }

            _context.UserOAuths.Remove(googleLink);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            _context.UserLogs.Add(new UserLog { UserId = user.Id, Action = "Google Account Unlinked", IPAddress = ip });
            await _context.SaveChangesAsync();

            return Ok(new { message = "Google account unlinked successfully." });
        }

        [HttpPost("request-delete")]
        public async Task<ActionResult> RequestAccountDeletion()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (user.DeleteRequestedAt.HasValue)
            {
                return BadRequest($"Deletion already requested. Your account will be deleted on {user.DeleteRequestedAt.Value.AddDays(14):yyyy-MM-dd}.");
            }

            user.DeleteRequestedAt = DateTime.UtcNow;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            _context.UserLogs.Add(new UserLog { UserId = user.Id, Action = "Account Deletion Requested", IPAddress = ip });
            await _context.SaveChangesAsync();

            return Ok(new { message = "Account deletion request submitted.", scheduledDeletionDate = user.DeleteRequestedAt.Value.AddDays(14) });
        }

        [HttpPost("cancel-delete")]
        public async Task<ActionResult> CancelAccountDeletion()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (!user.DeleteRequestedAt.HasValue)
            {
                return BadRequest("No deletion request is pending.");
            }

            user.DeleteRequestedAt = null;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            _context.UserLogs.Add(new UserLog { UserId = user.Id, Action = "Account Deletion Cancelled", IPAddress = ip });
            await _context.SaveChangesAsync();

            return Ok(new { message = "Account deletion request cancelled." });
        }

        private void DeleteLocalAvatarFile(string? avatarUrl)
        {
            if (string.IsNullOrEmpty(avatarUrl)) return;

            // Only delete if it's a local path (starts with /uploads/)
            if (avatarUrl.StartsWith("/uploads/"))
            {
                var filePath = Path.Combine(_environment.WebRootPath, avatarUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    try { System.IO.File.Delete(filePath); } catch { /* Ignore */ }
                }
            }
        }

        private static async Task<bool> IsSupportedAvatarAsync(IFormFile file)
        {
            if (file.Length is <= 0 or > 2_000_000) return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp")) return false;

            var header = new byte[12];
            await using var stream = file.OpenReadStream();
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
            if (read < 4) return false;

            var jpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            var png = read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            var webp = read >= 12 && Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && Encoding.ASCII.GetString(header, 8, 4) == "WEBP";
            return jpeg || png || webp;
        }

        private async Task DestroyAccountData(User user)
        {
            // Delete the local avatar file from disk if present
            DeleteLocalAvatarFile(user.AvatarUrl);

            // Anonymize all personally identifiable information
            var memberRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Member");
            user.RemoteUser = $"[deleted_{user.Id.ToString()[..8]}]";
            user.Email = null;
            user.DisplayName = "[deleted]";
            user.AvatarUrl = null;
            user.RemotePassword = null;
            if (memberRole != null) user.RoleId = memberRole.Id;
            user.DeleteRequestedAt = null;

            // Remove all OAuth links for this user (Quota cleanup)
            var oauthLinks = _context.UserOAuths.Where(uo => uo.UserId == user.Id);
            _context.UserOAuths.RemoveRange(oauthLinks);

            // Remove all portfolios (this will cascade delete Transactions and CashWallets)
            var portfolios = _context.Portfolios.Where(p => p.UserId == user.Id);
            _context.Portfolios.RemoveRange(portfolios);

            // Remove Watchlist entries if they exist
            var watchlists = _context.Watchlists.Where(w => w.UserId == user.Id);
            _context.Watchlists.RemoveRange(watchlists);

            _context.UserLogs.Add(new UserLog { UserId = user.Id, Action = "Account Permanently Destroyed", IPAddress = "system" });
            await _context.SaveChangesAsync();
        }

        // ── Helper: load permissions for a user based on their role ────────────
        private async Task<List<string>> GetUserPermissions(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r!.RolePermissions)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Role?.RolePermissions == null) return new List<string>();

            return user.Role.RolePermissions
                .Select(rp => rp.PermissionKey)
                .ToList();
        }
 
        private string GenerateJwtToken(User user)
        {
            var claims = new[] {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.RemoteUser)
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:DurationInMinutes"] ?? "15")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string HashToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        private async Task<string> GenerateAndSaveRefreshToken(User user)
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var refreshTokenString = Convert.ToBase64String(randomNumber);

            var refreshToken = new RefreshToken
            {
                Token = HashToken(refreshTokenString),
                UserId = user.Id,
                Expires = DateTime.UtcNow.AddDays(7), // 7 days expiry
                Created = DateTime.UtcNow
            };

            var oldTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && (rt.Revoked != null || rt.Expires <= DateTime.UtcNow))
                .ToListAsync();
            _context.RefreshTokens.RemoveRange(oldTokens);

            var excessActiveTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.Revoked == null && rt.Expires > DateTime.UtcNow)
                .OrderByDescending(rt => rt.Created)
                .Skip(4)
                .ToListAsync();
            foreach (var token in excessActiveTokens) token.Revoked = DateTime.UtcNow;

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return refreshTokenString;
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult> RefreshToken(RefreshTokenRequest request)
        {
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == HashToken(request.RefreshToken));

            if (refreshToken == null || !refreshToken.IsActive)
            {
                return Unauthorized("Invalid or expired refresh token");
            }

            if (!refreshToken.User.IsActive ||
                (refreshToken.User.BannedUntil.HasValue && refreshToken.User.BannedUntil > DateTime.UtcNow))
            {
                refreshToken.Revoked = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Unauthorized("This account is not permitted to refresh tokens.");
            }

            // Revoke the old token (token rotation)
            refreshToken.Revoked = DateTime.UtcNow;
            _context.RefreshTokens.Update(refreshToken);

            // Generate new pair
            var newAccessToken = GenerateJwtToken(refreshToken.User);
            var newRefreshToken = await GenerateAndSaveRefreshToken(refreshToken.User);
            var refreshPerms = await GetUserPermissions(refreshToken.User.Id);

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken,
                userId = refreshToken.User.Id,
                expiresIn = 60,
                permissions = refreshPerms
            });
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        [EnableRateLimiting("password-reset")]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                // For security, don't reveal if user exists. Just say email sent.
                return Ok(new { message = "If the email is registered, a reset link has been sent." });
            }

            if (!string.IsNullOrEmpty(user.PasswordResetToken) && user.PasswordResetTokenExpiry > DateTime.UtcNow)
                return Ok(new { message = "If the email is registered, a reset link has been sent." });

            // Generate Token
            var token = Guid.NewGuid().ToString("N");
            user.PasswordResetToken = HashToken(token);
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // 1 hour expiry

            await _context.SaveChangesAsync();

            // Send Email
            var frontendUrl = _config["FrontendUrl"] ?? "http://localhost:5173";
            var resetLink = $"{frontendUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email!)}";
            await _emailService.SendPasswordResetEmailAsync(user.Email!, resetLink);

            return Ok(new { message = "If the email is registered, a reset link has been sent." });
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult> ResetPassword(ResetPasswordRequest request)
        {
            var tokenHash = HashToken(request.Token);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.PasswordResetToken == tokenHash);

            if (user == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                return BadRequest("Invalid or expired reset token.");
            }

            // Update Password
            user.RemotePassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordResetToken = null; // Consume token
            user.PasswordResetTokenExpiry = null;

            var resetActiveTokens = await _context.RefreshTokens.Where(rt => rt.UserId == user.Id && rt.Revoked == null).ToListAsync();
            foreach (var token in resetActiveTokens) token.Revoked = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password has been successfully reset." });
        }
    }
}
