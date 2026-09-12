using _3TaC8_PlanningPort.Data;
using _3TaC8_PlanningPort.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace _3TaC8_PlanningPort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CashController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CashController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<bool> OwnsPortfolioAsync(Guid portfolioId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdValue, out var userId) &&
                await _context.Portfolios.AnyAsync(p => p.Id == portfolioId && p.UserId == userId);
        }

        private async Task<CashWallet> GetOrCreateWallet(Guid portfolioId)
        {
            var wallet = await _context.CashWallets.FirstOrDefaultAsync(w => w.PortfolioId == portfolioId);
            if (wallet == null)
            {
                wallet = new CashWallet { PortfolioId = portfolioId, Balance = 0 };
                _context.CashWallets.Add(wallet);
                await _context.SaveChangesAsync();
            }
            return wallet;
        }

        [HttpGet("{portfolioId}")]
        public async Task<ActionResult> GetBalance(Guid portfolioId)
        {
            if (!await OwnsPortfolioAsync(portfolioId)) return Forbid();
            var wallet = await GetOrCreateWallet(portfolioId);
            return Ok(wallet);
        }

        [HttpPost("deposit")]
        [EnableRateLimiting("write")]
        public async Task<ActionResult> Deposit([FromQuery] Guid portfolioId, [FromQuery] decimal amount)
        {
            if (!await OwnsPortfolioAsync(portfolioId)) return Forbid();
            if (amount <= 0 || amount > 1_000_000_000_000m) return BadRequest("Amount is outside the supported range");
            
            var wallet = await GetOrCreateWallet(portfolioId);
            wallet.Balance += amount;
            wallet.TotalDeposited += amount;
            wallet.LastUpdated = DateTime.UtcNow;

            var tx = new Transaction
            {
                Id = Guid.NewGuid(),
                PortfolioId = portfolioId,
                Symbol = "CASH",
                Type = "Deposit",
                Quantity = amount,
                PricePerUnit = 1,
                Currency = "USD",
                AssetType = "Cash",
                Subtype = "Cash Deposit",
                Exchange = "WALLET",
                TransactionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.Transactions.Add(tx);

            await _context.SaveChangesAsync();
            return Ok(wallet);
        }

        [HttpPost("withdraw")]
        [EnableRateLimiting("write")]
        public async Task<ActionResult> Withdraw([FromQuery] Guid portfolioId, [FromQuery] decimal amount)
        {
            if (!await OwnsPortfolioAsync(portfolioId)) return Forbid();
            if (amount <= 0 || amount > 1_000_000_000_000m) return BadRequest("Amount is outside the supported range");
            
            var wallet = await GetOrCreateWallet(portfolioId);
            if (wallet.Balance < amount) return BadRequest("Insufficient balance");

            wallet.Balance -= amount;
            wallet.TotalWithdrawn += amount;
            wallet.LastUpdated = DateTime.UtcNow;

            var tx = new Transaction
            {
                Id = Guid.NewGuid(),
                PortfolioId = portfolioId,
                Symbol = "CASH",
                Type = "Withdraw",
                Quantity = amount,
                PricePerUnit = 1,
                Currency = "USD",
                AssetType = "Cash",
                Subtype = "Cash Withdrawal",
                Exchange = "WALLET",
                TransactionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.Transactions.Add(tx);

            await _context.SaveChangesAsync();
            return Ok(wallet);
        }
    }
}
