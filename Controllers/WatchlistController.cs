using _3TaC8_PlanningPort.Data;
using _3TaC8_PlanningPort.Entities;
using _3TaC8_PlanningPort.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace _3TaC8_PlanningPort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WatchlistController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly StockService _stockService;

        public WatchlistController(ApplicationDbContext context, StockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        private bool IsCurrentUser(Guid userId) =>
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId) && currentUserId == userId;

        // 1. เพิ่มหุ้นเข้า Watchlist [cite: 2026-04-01]
        [HttpPost("add")]
        [EnableRateLimiting("expensive")]
        public async Task<IActionResult> AddToWatchlist(Guid userId, string symbol, string exchange = "NASDAQ")
        {
            if (!IsCurrentUser(userId)) return Forbid();
            if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(exchange) ||
                !Regex.IsMatch(symbol, @"^[A-Za-z0-9._-]{1,20}$") ||
                !Regex.IsMatch(exchange, @"^[A-Za-z0-9._-]{1,20}$"))
                return BadRequest("Invalid symbol or exchange.");
            if (await _context.Watchlists.CountAsync(w => w.UserId == userId) >= 50)
                return BadRequest("Watchlist limit reached.");

            var upperSymbol = symbol.ToUpper();
            var upperExchange = exchange.ToUpper();

            // เช็กว่ามีอยู่ในลิสต์หรือยัง (ตรวจ Exchange ด้วย)
            var exists = await _context.Watchlists
                .AnyAsync(w => w.UserId == userId && w.Symbol == upperSymbol && w.Exchange == upperExchange);

            if (exists) return BadRequest($"{upperExchange}:{upperSymbol} is already in your watchlist.");

            // ✨ Validation: เช็กว่าหุ้นตัวนี้มีอยู่จริงไหม (ผ่านราคา) [NEW]
            var priceData = await _stockService.GetCurrentPriceAsync(upperSymbol, upperExchange);
            if (priceData.CurrentPrice <= 0)
            {
                return BadRequest($"Symbol '{upperSymbol}' was not found on {upperExchange}. Please check again.");
            }

            var watchItem = new Watchlist
            {
                UserId = userId,
                Symbol = upperSymbol,
                Exchange = upperExchange
            };

            _context.Watchlists.Add(watchItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{upperExchange}:{upperSymbol} added to watchlist" });
        }

        // 2. ดึงรายการ Watchlist ทั้งหมดพร้อมราคาล่าสุด [cite: 2026-04-01]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetMyWatchlist(Guid userId)
        {
            if (!IsCurrentUser(userId)) return Forbid();
            var list = await _context.Watchlists
                .Where(w => w.UserId == userId)
                .ToListAsync();

            var results = new List<object>();

            foreach (var item in list)
            {
                // ส่ง Exchange ให้ Service ด้วยเพื่อดึงราคาที่ถูกต้อง [cite: 2026-04-02]
                var priceData = await _stockService.GetPriceWithSnapshotAsync(item.Symbol, item.Exchange);
                
                results.Add(new
                {
                    item.Id, // ✨ ส่ง ID ออกไปด้วยเพื่อให้ลบได้แม่นยำ [NEW]
                    item.Symbol,
                    item.Exchange,
                    FullSymbol = $"{item.Exchange}:{item.Symbol}", // e.g. "NASDAQ:AAPL"
                    CurrentPrice = priceData.CurrentPrice,
                    DailyChange = priceData.Change,
                    DailyPercentChange = priceData.PercentChange,
                    AddedAt = item.AddedAt
                });
            }

            return Ok(results);
        }

        // 3. ลบหุ้นออกจาก Watchlist (ใช้ ID เพื่อความแม่นยำสูงสุด) [cite: 2026-04-07]
        [HttpDelete("remove/{id:guid}")]
        [EnableRateLimiting("write")]
        public async Task<IActionResult> RemoveFromWatchlist(Guid id)
        {
            var item = await _context.Watchlists.FindAsync(id);

            if (item == null) return NotFound("ไม่พบรายการนี้ใน Watchlist");
            if (!IsCurrentUser(item.UserId)) return Forbid();

            _context.Watchlists.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Removed successfully" });
        }
    }
}
