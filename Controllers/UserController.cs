using _3TaC8_PlanningPort.Data;
using _3TaC8_PlanningPort.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using _3TaC8_PlanningPort.DTOs;

namespace _3TaC8_PlanningPort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
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
                    // เข้ารหัสก่อนลง DB !!
                    RemotePassword = BCrypt.Net.BCrypt.HashPassword(request.RemotePassword)
                };
                _context.Users.Add(newUser);
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

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.RemotePassword, user.RemotePassword))
            {
                return Unauthorized("Invalid username or password");
            }

            return Ok(new { message = "Login successful", userId = user.Id });
        }
    }
}