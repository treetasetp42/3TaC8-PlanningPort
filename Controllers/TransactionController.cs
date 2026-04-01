using _3TaC8_PlanningPort.Data;
using _3TaC8_PlanningPort.DTOs;
using _3TaC8_PlanningPort.Entities;
using _3TaC8_PlanningPort.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _3TaC8_PlanningPort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly StockService _stockService;
        public TransactionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/Transaction/add
        [HttpPost("add")]
        public async Task<ActionResult> AddTransaction(Guid userId, TransactionRequest request)
        {
            var transaction = new Transaction
            {
                UserId = userId,
                Symbol = request.Symbol.ToUpper(),
                Type = request.Type,
                Quantity = request.Quantity,
                PricePerUnit = request.PricePerUnit,
                Currency = request.Currency,
                AssetType = request.AssetType,
                Subtype = request.Subtype,
                TransactionDate = request.TransactionDate
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Transaction recorded successfully" });
        }

        // GET: api/Transaction/summary/{userId}/{symbol}
        // ใช้คำนวณค่าเฉลี่ยตามข้อ 5 ของคุณ
        [HttpGet("summary/{userId}/{symbol}")]
        public async Task<ActionResult> GetStockSummary(Guid userId, string symbol)
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId && t.Symbol == symbol.ToUpper())
                .ToListAsync();

            if (!transactions.Any()) return NotFound("No transactions found for this symbol");

            var totalQty = transactions.Where(t => t.Type == "Buy").Sum(t => t.Quantity)
                         - transactions.Where(t => t.Type == "Sell").Sum(t => t.Quantity);

            var totalCost = transactions.Where(t => t.Type == "Buy").Sum(t => t.Quantity * t.PricePerUnit);
            var avgPrice = totalQty > 0 ? totalCost / transactions.Where(t => t.Type == "Buy").Sum(t => t.Quantity) : 0;

            return Ok(new
            {
                Symbol = symbol.ToUpper(),
                CurrentQuantity = totalQty,
                AverageCost = avgPrice,
                TotalInvestment = totalCost
            });
        }
        // GET: api/Transaction/portfolio/{userId}/{symbol}
        [HttpGet("portfolio/{userId}/{symbol}")]
        public async Task<ActionResult> GetPortfolioDetail(Guid userId, string symbol)
        {
            // 1. ดึงประวัติธุรกรรมจาก DB [cite: 2026-04-01]
            var txs = await _context.Transactions
                .Where(t => t.UserId == userId && t.Symbol == symbol.ToUpper())
                .ToListAsync();

            if (!txs.Any()) return NotFound("ไม่พบข้อมูลหุ้นตัวนี้ในพอร์ตของคุณ");

            // 2. คำนวณจำนวนหุ้นและต้นทุน [cite: 2026-04-01]
            var buyQty = txs.Where(t => t.Type == "Buy").Sum(t => t.Quantity);
            var sellQty = txs.Where(t => t.Type == "Sell").Sum(t => t.Quantity);
            var currentQty = buyQty - sellQty;

            var totalCost = txs.Where(t => t.Type == "Buy").Sum(t => t.Quantity * t.PricePerUnit);
            var avgCost = buyQty > 0 ? totalCost / buyQty : 0;

            // 3. ดึงราคา Real-time จาก Finnhub API [cite: 2026-04-01]
            var marketPrice = await _stockService.GetCurrentPriceAsync(symbol);

            // 4. คำนวณ Profit / Loss [cite: 2026-04-01]
            var currentValue = currentQty * marketPrice;
            var profitLoss = currentValue - (currentQty * avgCost);
            var profitPercentage = avgCost > 0 ? (marketPrice - avgCost) / avgCost * 100 : 0;

            return Ok(new
            {
                Symbol = symbol.ToUpper(),
                Holdings = currentQty,
                AverageCost = avgCost,
                MarketPrice = marketPrice,
                CurrentValue = currentValue,
                ProfitLoss = profitLoss,
                ProfitPercentage = Math.Round(profitPercentage, 2) + "%"
            });
        }
    }
}