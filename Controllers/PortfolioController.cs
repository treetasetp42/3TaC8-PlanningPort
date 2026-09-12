using _3TaC8_PlanningPort.Data;
using _3TaC8_PlanningPort.DTOs;
using _3TaC8_PlanningPort.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace _3TaC8_PlanningPort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PortfolioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PortfolioController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Portfolio/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<IEnumerable<Portfolio>>> GetPortfolios(Guid userId)
        {
            // Security check: ensure requesting user matches
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserIdStr != userId.ToString()) return Unauthorized();

            var portfolios = await _context.Portfolios
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            return Ok(portfolios);
        }

        // POST: api/Portfolio/add
        [HttpPost("add")]
        [EnableRateLimiting("write")]
        public async Task<ActionResult> AddPortfolio([FromQuery] Guid userId, [FromBody] PortfolioRequest request)
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserIdStr != userId.ToString()) return Unauthorized();

            var count = await _context.Portfolios.CountAsync(p => p.UserId == userId);
            // Limit to 10 portfolios as per requirement
            if (count >= 10) return BadRequest("You have reached the limit of 10 portfolios.");

            var portfolio = new Portfolio
            {
                UserId = userId,
                Name = request.Name,
                Description = request.Description,
                ColorCode = request.ColorCode ?? "#6C5DD3"
            };

            _context.Portfolios.Add(portfolio);
            await _context.SaveChangesAsync();

            return Ok(portfolio);
        }

        // PUT: api/Portfolio/update/{portfolioId}
        [HttpPut("update/{portfolioId}")]
        [EnableRateLimiting("write")]
        public async Task<ActionResult> UpdatePortfolio(Guid portfolioId, [FromBody] PortfolioRequest request)
        {
            var portfolio = await _context.Portfolios.FindAsync(portfolioId);
            if (portfolio == null) return NotFound("Portfolio not found");

            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserIdStr != portfolio.UserId.ToString()) return Unauthorized();

            portfolio.Name = request.Name;
            portfolio.Description = request.Description;
            if (request.ColorCode != null) portfolio.ColorCode = request.ColorCode;

            await _context.SaveChangesAsync();
            return Ok(portfolio);
        }

        // DELETE: api/Portfolio/delete/{portfolioId}
        [HttpDelete("delete/{portfolioId}")]
        [EnableRateLimiting("write")]
        public async Task<ActionResult> DeletePortfolio(Guid portfolioId)
        {
            var portfolio = await _context.Portfolios.FindAsync(portfolioId);
            if (portfolio == null) return NotFound("Portfolio not found");

            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserIdStr != portfolio.UserId.ToString()) return Unauthorized();

            // Check if it's the last portfolio. We shouldn't delete the last one.
            var count = await _context.Portfolios.CountAsync(p => p.UserId == portfolio.UserId);
            if (count <= 1) return BadRequest("You cannot delete your only portfolio. Create another one first.");

            _context.Portfolios.Remove(portfolio);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Portfolio deleted safely" });
        }
    }

    public class PortfolioRequest 
    {
        [Required, StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(255)]
        public string? Description { get; set; }
        [RegularExpression("^#[0-9A-Fa-f]{6}$")]
        public string? ColorCode { get; set; }
    }
}
