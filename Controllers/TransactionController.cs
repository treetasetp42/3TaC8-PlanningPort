using _3TaC8_PlanningPort.Data;
using _3TaC8_PlanningPort.DTOs;
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
    public class TransactionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly StockService _stockService;
        public TransactionController(ApplicationDbContext context, StockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        private async Task<bool> OwnsPortfolioAsync(Guid portfolioId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdValue, out var userId) &&
                await _context.Portfolios.AnyAsync(p => p.Id == portfolioId && p.UserId == userId);
        }

        private static bool IsValidSymbol(string symbol) =>
            !string.IsNullOrWhiteSpace(symbol) && Regex.IsMatch(symbol, @"^[A-Za-z0-9._-]{1,20}$");

        // POST: api/Transaction/add
        [HttpPost("add")]
        [EnableRateLimiting("write")]
        public async Task<ActionResult> AddTransaction([FromQuery] Guid portfolioId, [FromBody] TransactionRequest request)
        {
            if (!await OwnsPortfolioAsync(portfolioId)) return Forbid();
            if (await _context.Transactions.CountAsync(t => t.PortfolioId == portfolioId) >= 5_000)
                return BadRequest("Transaction limit reached for this portfolio.");

            var upperSymbol = request.Symbol.ToUpperInvariant();
            var upperExchange = request.Exchange.ToUpperInvariant();
            var symbolExists = await _context.Transactions.AnyAsync(t =>
                t.PortfolioId == portfolioId && t.Symbol == upperSymbol && t.Exchange == upperExchange);
            if (!symbolExists)
            {
                var distinctSymbols = await _context.Transactions
                    .Where(t => t.PortfolioId == portfolioId)
                    .Select(t => new { t.Symbol, t.Exchange })
                    .Distinct()
                    .CountAsync();
                if (distinctSymbols >= 50)
                    return BadRequest("Asset limit reached for this portfolio.");
            }

            var wallet = await _context.CashWallets.FirstOrDefaultAsync(w => w.PortfolioId == portfolioId);
            if (wallet == null)
            {
                wallet = new CashWallet { PortfolioId = portfolioId, Balance = 0 };
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
                    .Where(t => t.PortfolioId == portfolioId && t.Symbol == upperSymbol && t.Exchange == upperExchange)
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
                PortfolioId = portfolioId,
                Symbol = upperSymbol,
                Type = request.Type,
                Quantity = request.Quantity,
                PricePerUnit = request.PricePerUnit,
                Currency = request.Currency,
                AssetType = request.AssetType,
                Subtype = request.Subtype,
                Exchange = upperExchange,
                TransactionDate = request.TransactionDate
            };

            _context.Transactions.Add(transaction);
            wallet.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Transaction recorded and wallet updated successfully" });
        }

        // GET: api/Transaction/summary/{portfolioId}/{symbol}
        [HttpGet("summary/{portfolioId}/{symbol}")]
        public async Task<ActionResult> GetStockSummary(Guid portfolioId, string symbol)
        {
            if (!await OwnsPortfolioAsync(portfolioId)) return Forbid();
            if (!IsValidSymbol(symbol)) return BadRequest("Invalid symbol.");
            var transactions = await _context.Transactions
                .Where(t => t.PortfolioId == portfolioId && t.Symbol == symbol.ToUpper())
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

        // GET: api/Transaction/portfolio/{portfolioId}/{symbol}
        [HttpGet("portfolio/{portfolioId}/{symbol}")]
        [EnableRateLimiting("expensive")]
        public async Task<ActionResult> GetPortfolioDetail(Guid portfolioId, string symbol)
        {
            if (!await OwnsPortfolioAsync(portfolioId)) return Forbid();
            if (!IsValidSymbol(symbol)) return BadRequest("Invalid symbol.");
            var txs = await _context.Transactions
                .Where(t => t.PortfolioId == portfolioId && t.Symbol == symbol.ToUpper())
                .ToListAsync();

            if (!txs.Any()) return NotFound("ไม่พบข้อมูลหุ้นตัวนี้ในพอร์ตของคุณ");

            var buyQty = txs.Where(t => t.Type == "Buy").Sum(t => t.Quantity);
            var sellQty = txs.Where(t => t.Type == "Sell").Sum(t => t.Quantity);
            var currentQty = buyQty - sellQty;

            var totalCost = txs.Where(t => t.Type == "Buy").Sum(t => t.Quantity * t.PricePerUnit);
            var avgCost = buyQty > 0 ? totalCost / buyQty : 0;

            var priceData = await _stockService.GetPriceWithSnapshotAsync(symbol);  
            var marketPrice = priceData.CurrentPrice;

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

        [HttpGet("history/{portfolioId}")]
        public async Task<ActionResult> GetTransactionHistory(Guid portfolioId)
        {
            if (!await OwnsPortfolioAsync(portfolioId)) return Forbid();
            var txs = await _context.Transactions
                .Where(t => t.PortfolioId == portfolioId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            return Ok(txs);
        }

        // GET: api/Transaction/dashboard/{portfolioId}
        [HttpGet("dashboard/{portfolioId}")]
        [EnableRateLimiting("expensive")]
        public async Task<IActionResult> GetDashboardSummary(Guid portfolioId)
        {
            if (!await OwnsPortfolioAsync(portfolioId)) return Forbid();
            var allTxs = await _context.Transactions
                .Where(t => t.PortfolioId == portfolioId)
                .ToListAsync();

            if (!allTxs.Any()) return NotFound("ไม่พบข้อมูลการลงทุน");

            var portfolioItems = allTxs.GroupBy(t => new { t.Symbol, t.Exchange })
                .Select(g => {
                    var buyQty = g.Where(t => t.Type == "Buy").Sum(t => t.Quantity);
                    var buyCost = g.Where(t => t.Type == "Buy").Sum(t => t.Quantity * t.PricePerUnit);
                    var avgCost = buyQty > 0 ? buyCost / buyQty : 0;
                    var currentQty = buyQty - g.Where(t => t.Type == "Sell").Sum(t => t.Quantity);

                    return new {
                        Symbol = g.Key.Symbol,
                        Exchange = g.Key.Exchange,
                        AssetType = g.First().AssetType,
                        Subtype = g.First().Subtype,
                        TotalQty = currentQty,
                        TotalCost = avgCost * currentQty, // active cost basis
                        AvgCost = avgCost
                    };
                })
                .Where(x => x.TotalQty > 0)
                .ToList();

            var summaryList = new List<object>();
            decimal totalPortfolioValue = 0;
            decimal totalProfitLoss = 0;

            foreach (var item in portfolioItems)
            {
                var priceData = await _stockService.GetPriceWithSnapshotAsync(item.Symbol, item.Exchange);
                var currentPrice = priceData.CurrentPrice;
                var currentValue = item.TotalQty * currentPrice;
                var profitLoss = currentValue - item.TotalCost; 
 
                totalPortfolioValue += currentValue;
                totalProfitLoss += profitLoss; 
 
                summaryList.Add(new
                {
                    item.Symbol,
                    item.Exchange,
                    item.AssetType,
                    item.Subtype,
                    Holdings = item.TotalQty,
                    AverageCost = item.AvgCost,
                    CurrentValue = currentValue,
                    CurrentPrice = currentPrice,
                    ProfitLoss = profitLoss
                });
            }
 
            decimal totalInvestment = portfolioItems.Sum(x => x.TotalCost);

            var allocation = summaryList.Cast<dynamic>()
                .GroupBy(s => s.AssetType)
                .Select(g => new {
                    Type = g.Key,
                    Value = g.Sum(x => (decimal)x.CurrentValue),
                    Percentage = totalPortfolioValue > 0
                        ? Math.Round(g.Sum(x => (decimal)x.CurrentValue) / totalPortfolioValue * 100, 2) + "%"
                        : "0%"
                });

            var wallet = await _context.CashWallets.FirstOrDefaultAsync(w => w.PortfolioId == portfolioId);
            decimal cashBalance = wallet?.Balance ?? 0;
            decimal totalRealizedProfit = wallet?.TotalRealizedProfit ?? 0;

            return Ok(new
            {
                TotalValue = totalPortfolioValue + cashBalance,
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
        [EnableRateLimiting("write")]
        public async Task<ActionResult> UpdateTransaction([FromQuery] Guid portfolioId, [FromQuery] string symbol, [FromBody] TransactionRequest request)
        {
            if (!await OwnsPortfolioAsync(portfolioId)) return Forbid();
            if (!IsValidSymbol(symbol)) return BadRequest("Invalid symbol.");
            var latestTx = await _context.Transactions
                .Where(t => t.PortfolioId == portfolioId && t.Symbol == symbol.ToUpper())
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefaultAsync();
 
            if (latestTx == null) return NotFound("Transaction not found");

            var wallet = await _context.CashWallets.FirstOrDefaultAsync(w => w.PortfolioId == portfolioId);
            if (wallet != null)
            {
                // 1. Reverse old impact
                if (latestTx.Type == "Buy") wallet.Balance += (latestTx.Quantity * latestTx.PricePerUnit);
                else if (latestTx.Type == "Sell") wallet.Balance -= (latestTx.Quantity * latestTx.PricePerUnit);

                // 2. Apply new impact
                if (request.Type == "Buy") wallet.Balance -= (request.Quantity * request.PricePerUnit);
                else if (request.Type == "Sell") wallet.Balance += (request.Quantity * request.PricePerUnit);
            }
 
            latestTx.Quantity = request.Quantity;
            latestTx.PricePerUnit = request.PricePerUnit;
            latestTx.Type = request.Type; // Allow changing type
            latestTx.Subtype = request.Subtype;
            latestTx.AssetType = request.AssetType;
            latestTx.Exchange = request.Exchange.ToUpper();
            latestTx.TransactionDate = DateTime.UtcNow;
 
            await _context.SaveChangesAsync();
            return Ok(new { message = "Transaction updated and wallet reconciled successfully" });
        }
 
        // DELETE: api/Transaction/delete
        [HttpDelete("delete")]
        [EnableRateLimiting("write")]
        public async Task<ActionResult> DeleteTransaction([FromQuery] Guid portfolioId, [FromQuery] string symbol)
        {
            if (!await OwnsPortfolioAsync(portfolioId)) return Forbid();
            if (!IsValidSymbol(symbol)) return BadRequest("Invalid symbol.");
            var txs = await _context.Transactions
                .Where(t => t.PortfolioId == portfolioId && t.Symbol == symbol.ToUpper())
                .ToListAsync();
 
            if (!txs.Any()) return NotFound("No transactions found to delete");

            var wallet = await _context.CashWallets.FirstOrDefaultAsync(w => w.PortfolioId == portfolioId);
            if (wallet != null)
            {
                foreach (var tx in txs)
                {
                    if (tx.Type == "Buy") wallet.Balance += (tx.Quantity * tx.PricePerUnit);
                    else if (tx.Type == "Sell") wallet.Balance -= (tx.Quantity * tx.PricePerUnit);
                }
            }
 
            _context.Transactions.RemoveRange(txs);
            await _context.SaveChangesAsync();
 
            return Ok(new { message = "Transactions deleted and cash refunded successfully" });
        }
    }
}
