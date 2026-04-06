using _3TaC8_PlanningPort.Data;
using _3TaC8_PlanningPort.Entities;
using _3TaC8_PlanningPort.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _3TaC8_PlanningPort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WatchlistController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly StockService _stockService;

        public WatchlistController(ApplicationDbContext context, StockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        // 1. เพิ่มหุ้นเข้า Watchlist [cite: 2026-04-01]
        [HttpPost("add")]
        public async Task<IActionResult> AddToWatchlist(Guid userId, string symbol, string exchange = "NASDAQ")
        {
            var upperSymbol = symbol.ToUpper();
            var upperExchange = exchange.ToUpper();

            // เช็กว่ามีอยู่ในลิสต์หรือยัง (ตรวจ Exchange ด้วย)
            var exists = await _context.Watchlists
                .AnyAsync(w => w.UserId == userId && w.Symbol == upperSymbol && w.Exchange == upperExchange);

            if (exists) return BadRequest($"{upperExchange}:{upperSymbol} is already in your watchlist.");

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
            var list = await _context.Watchlists
                .Where(w => w.UserId == userId)
                .ToListAsync();

            var results = new List<object>();

            foreach (var item in list)
            {
                // ส่ง Exchange ให้ Service ด้วยเพื่อดึงราคาที่ถูกต้อง [cite: 2026-04-02]
                var price = await _stockService.GetPriceWithSnapshotAsync(item.Symbol, item.Exchange);

                results.Add(new
                {
                    item.Symbol,
                    item.Exchange,
                    FullSymbol = $"{item.Exchange}:{item.Symbol}", // e.g. "NASDAQ:AAPL"
                    CurrentPrice = price,
                    AddedAt = item.AddedAt
                });
            }

            return Ok(results);
        }

        // 3. ลบหุ้นออกจาก Watchlist [cite: 2026-04-01]
        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveFromWatchlist(Guid userId, string symbol, string exchange = "NASDAQ")
        {
            var item = await _context.Watchlists
                .FirstOrDefaultAsync(w =>
                    w.UserId == userId &&
                    w.Symbol == symbol.ToUpper() &&
                    w.Exchange == exchange.ToUpper());

            if (item == null) return NotFound("ไม่พบหุ้นตัวนี้ใน Watchlist");

            _context.Watchlists.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Removed successfully" });
        }
    }
}