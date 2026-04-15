using _3TaC8_PlanningPort.Data;
using _3TaC8_PlanningPort.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _3TaC8_PlanningPort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CashController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CashController(ApplicationDbContext context)
        {
            _context = context;
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
            var wallet = await GetOrCreateWallet(portfolioId);
            return Ok(wallet);
        }

        [HttpPost("deposit")]
        public async Task<ActionResult> Deposit([FromQuery] Guid portfolioId, [FromQuery] decimal amount)
        {
            if (amount <= 0) return BadRequest("Amount must be positive");
            
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
        public async Task<ActionResult> Withdraw([FromQuery] Guid portfolioId, [FromQuery] decimal amount)
        {
            if (amount <= 0) return BadRequest("Amount must be positive");
            
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
