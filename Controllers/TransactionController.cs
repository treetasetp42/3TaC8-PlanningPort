using _3TaC8_PlanningPort.Data;
using _3TaC8_PlanningPort.Entities;
using _3TaC8_PlanningPort.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _3TaC8_PlanningPort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

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
    }
}