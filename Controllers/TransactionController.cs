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
            var wallet = await _context.CashWallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                wallet = new CashWallet { UserId = userId, Balance = 0 };
                _context.CashWallets.Add(wallet);
            }

            decimal totalAmount = request.Quantity * request.PricePerUnit;

            if (request.Type == "Buy")
            {
                if (wallet.Balance < totalAmount) return BadRequest("Insufficient buying power (Cash Balance)");
                wallet.Balance -= totalAmount;
            }
            else if (request.Type == "Sell")
            {
                // Calculate Realized Profit based on Weighted Average Cost
                var txs = await _context.Transactions
                    .Where(t => t.UserId == userId && t.Symbol == request.Symbol.ToUpper() && t.Exchange == request.Exchange.ToUpper())
                    .ToListAsync();
                
                var totalBuyQty = txs.Where(t => t.Type == "Buy").Sum(t => t.Quantity);
                var totalBuyCost = txs.Where(t => t.Type == "Buy").Sum(t => t.Quantity * t.PricePerUnit);
                decimal avgCost = totalBuyQty > 0 ? totalBuyCost / totalBuyQty : 0;

                decimal realizedProfit = (request.PricePerUnit - avgCost) * request.Quantity;
                
                wallet.Balance += totalAmount;
                wallet.TotalRealizedProfit += realizedProfit;
            }

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
                Exchange = request.Exchange.ToUpper(),
                TransactionDate = request.TransactionDate
            };

            _context.Transactions.Add(transaction);
            wallet.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Transaction recorded and wallet updated successfully" });
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
            var priceData = await _stockService.GetPriceWithSnapshotAsync(symbol);  
            var marketPrice = priceData.CurrentPrice;

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
        [HttpGet("history/{userId}")]
        public async Task<ActionResult> GetTransactionHistory(Guid userId)
        {
            var txs = await _context.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            return Ok(txs);
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

            // 2. แยกกลุ่มตาม Symbol + Exchange เพื่อหาจำนวนที่ถือครองและต้นทุน [cite: 2026-04-01, 2026-04-02, 2026-04-09]
            var portfolioItems = allTxs.GroupBy(t => new { t.Symbol, t.Exchange })
                .Select(g => new {
                    Symbol = g.Key.Symbol,
                    Exchange = g.Key.Exchange,
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
            decimal totalProfitLoss = 0;

            // 3. ดึงราคาปัจจุบันมาคำนวณมูลค่ารวม (ใช้ Cache/API)  
            foreach (var item in portfolioItems)
            {
                var priceData = await _stockService.GetPriceWithSnapshotAsync(item.Symbol, item.Exchange);
                var currentPrice = priceData.CurrentPrice;
                var currentValue = item.TotalQty * currentPrice;
                var profitLoss = currentValue - item.TotalCost; // คำนวณรายตัว [cite: 2026-04-02]
 
                totalPortfolioValue += currentValue;
                totalProfitLoss += profitLoss; // 2. สะสมกำไรรวม [cite: 2026-04-02]
 
                summaryList.Add(new
                {
                    item.Symbol,
                    item.Exchange,
                    item.AssetType,
                    item.Subtype,
                    Holdings = item.TotalQty,
                    AverageCost = item.TotalQty > 0 ? item.TotalCost / item.TotalQty : 0,
                    CurrentValue = currentValue,
                    CurrentPrice = currentPrice,
                    ProfitLoss = profitLoss
                });
            }
 
            decimal totalInvestment = portfolioItems.Sum(x => x.TotalCost);

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

            var wallet = await _context.CashWallets.FirstOrDefaultAsync(w => w.UserId == userId);
            decimal cashBalance = wallet?.Balance ?? 0;
            decimal totalRealizedProfit = wallet?.TotalRealizedProfit ?? 0;

            return Ok(new
            {
                TotalValue = totalPortfolioValue + cashBalance, // Net Worth [cite: 2026-04-09]
                CashBalance = cashBalance,
                RealizedProfit = totalRealizedProfit,
                TotalInvestment = totalInvestment,
                AssetValue = totalPortfolioValue,
                TotalUnrealizedProfit = totalProfitLoss,
                Assets = summaryList,
                Allocation = allocation
            });
        }

        // PUT: api/Transaction/update
        [HttpPut("update")]
        public async Task<ActionResult> UpdateTransaction(Guid userId, string symbol, TransactionRequest request)
        {
            var latestTx = await _context.Transactions
                .Where(t => t.UserId == userId && t.Symbol == symbol.ToUpper())
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefaultAsync();
 
            if (latestTx == null) return NotFound("Transaction not found");
 
            latestTx.Quantity = request.Quantity;
            latestTx.PricePerUnit = request.PricePerUnit;
            latestTx.Subtype = request.Subtype;
            latestTx.AssetType = request.AssetType;
            latestTx.Exchange = request.Exchange.ToUpper();
            latestTx.TransactionDate = DateTime.UtcNow; // Update timestamp
 
            await _context.SaveChangesAsync();
            return Ok(new { message = "Transaction updated successfully" });
        }
 
        // DELETE: api/Transaction/delete
        [HttpDelete("delete")]
        public async Task<ActionResult> DeleteTransaction(Guid userId, string symbol)
        {
            var txs = await _context.Transactions
                .Where(t => t.UserId == userId && t.Symbol == symbol.ToUpper())
                .ToListAsync();
 
            if (!txs.Any()) return NotFound("No transactions found to delete");
 
            _context.Transactions.RemoveRange(txs);
            await _context.SaveChangesAsync();
 
            return NoContent();
        }
    }
}