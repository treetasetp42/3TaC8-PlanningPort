using _3TaC8_PlanningPort.Data;
using _3TaC8_PlanningPort.DTOs;
using _3TaC8_PlanningPort.Entities;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace _3TaC8_PlanningPort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public UserController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return Ok(new { message = "User created successfully", userId = user.Id });
            }
            catch (Exception ex)
            {
                // บรรทัดนี้จะช่วยให้เราเห็น Error จริงๆ ใน Swagger Response
                return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
            }
        }
        [HttpPost("register")]
        public async Task<ActionResult> Register(CreateUserRequest request)
        {
            try
            {
                var newUser = new User
                {
                    RemoteUser = request.RemoteUser,
                    RemotePassword = BCrypt.Net.BCrypt.HashPassword(request.RemotePassword)
                };
                _context.Users.Add(newUser);

                // บันทึก Log การสมัครสมาชิกใหม่ [cite: 2026-04-02]
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                _context.UserLogs.Add(new UserLog { UserId = newUser.Id, Action = "User Registered", IPAddress = ip });

                await _context.SaveChangesAsync();
                return Ok(new { message = "Registered successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(CreateUserRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RemoteUser == request.RemoteUser);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.RemotePassword, user.RemotePassword))
            {
                try
                {
                    // บันทึก Log เมื่อ Login พลาด [cite: 2026-04-02]
                    _context.UserLogs.Add(new UserLog { UserId = user?.Id ?? Guid.Empty, Action = "Login Failed", IPAddress = ip });
                }
                catch (Exception logEx)
                {
                    // ใส่ Breakpoint ตรงนี้เพื่อดูว่ามันพังเพราะอะไร [cite: 2026-04-02]
                    Console.WriteLine("Log failed: " + logEx.Message);
                }
                await _context.SaveChangesAsync();
                return Unauthorized("Invalid username or password");
            }

            // 1. สร้าง JWT Token [cite: 2026-04-01]
            var token = GenerateJwtToken(user);

            // 2. บันทึก Log เมื่อ Login สำเร็จ [cite: 2026-04-02]
            _context.UserLogs.Add(new UserLog { UserId = user.Id, Action = "Login Success", IPAddress = ip });
            await _context.SaveChangesAsync();

            return Ok(new
            {
                token = token,
                userId = user.Id,
                expiresIn = 60 // นาที [cite: 2026-04-01]
            });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[] {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.RemoteUser)
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(_config["Jwt:DurationInMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}