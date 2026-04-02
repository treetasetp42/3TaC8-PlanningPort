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
        public TransactionController(ApplicationDbContext context, StockService stockService)
        {
            _context = context;
            _stockService = stockService;
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
            var marketPrice = await _stockService.GetPriceWithSnapshotAsync(symbol);  

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
        // GET: api/Transaction/dashboard/{userId}
        [HttpGet("dashboard/{userId}")]
        public async Task<IActionResult> GetDashboardSummary(Guid userId)
        {
            // 1. ดึง Transactions ทั้งหมดของ User นี้ [cite: 2026-04-01]
            var allTxs = await _context.Transactions
                .Where(t => t.UserId == userId)
                .ToListAsync();

            if (!allTxs.Any()) return NotFound("ไม่พบข้อมูลการลงทุน");

            // 2. แยกกลุ่มตาม Symbol เพื่อหาจำนวนที่ถือครองและต้นทุน [cite: 2026-04-01, 2026-04-02]
            var portfolioItems = allTxs.GroupBy(t => t.Symbol)
                .Select(g => new {
                    Symbol = g.Key,
                    AssetType = g.First().AssetType,
                    Subtype = g.First().Subtype,
                    TotalQty = g.Where(t => t.Type == "Buy").Sum(t => t.Quantity) -
                               g.Where(t => t.Type == "Sell").Sum(t => t.Quantity),
                    TotalCost = g.Where(t => t.Type == "Buy").Sum(t => t.Quantity * t.PricePerUnit)
                })
                .Where(x => x.TotalQty > 0)
                .ToList();

            var summaryList = new List<object>();
            decimal totalPortfolioValue = 0;

            // 3. ดึงราคาปัจจุบันมาคำนวณมูลค่ารวม (ใช้ Cache/API) [cite: 2026-04-02]
            foreach (var item in portfolioItems)
            {
                var currentPrice = await _stockService.GetPriceWithSnapshotAsync(item.Symbol);
                var currentValue = item.TotalQty * currentPrice;
                totalPortfolioValue += currentValue;

                summaryList.Add(new
                {
                    item.Symbol,
                    item.AssetType,
                    item.Subtype,
                    Holdings = item.TotalQty,
                    CurrentValue = currentValue,
                    ProfitLoss = currentValue - item.TotalCost
                });
            }

            // 4. สรุปสัดส่วนตาม AssetType (Stock vs Crypto) [cite: 2026-03-20, 2026-04-01]
            var allocation = summaryList.Cast<dynamic>()
                .GroupBy(s => s.AssetType)
                .Select(g => new {
                    Type = g.Key,
                    Value = g.Sum(x => (decimal)x.CurrentValue),
                    Percentage = totalPortfolioValue > 0
                        ? Math.Round(g.Sum(x => (decimal)x.CurrentValue) / totalPortfolioValue * 100, 2) + "%"
                        : "0%"
                });

            return Ok(new
            {
                TotalValue = totalPortfolioValue,
                Assets = summaryList,
                Allocation = allocation
            });
        }

    }
}