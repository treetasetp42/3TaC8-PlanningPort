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

        private async Task<CashWallet> GetOrCreateWallet(Guid userId)
        {
            var wallet = await _context.CashWallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                wallet = new CashWallet { UserId = userId, Balance = 0 };
                _context.CashWallets.Add(wallet);
                await _context.SaveChangesAsync();
            }
            return wallet;
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult> GetBalance(Guid userId)
        {
            var wallet = await GetOrCreateWallet(userId);
            return Ok(wallet);
        }

        [HttpPost("deposit")]
        public async Task<ActionResult> Deposit(Guid userId, decimal amount)
        {
            if (amount <= 0) return BadRequest("Amount must be positive");
            
            var wallet = await GetOrCreateWallet(userId);
            wallet.Balance += amount;
            wallet.TotalDeposited += amount;
            wallet.LastUpdated = DateTime.UtcNow;

            // Log as transaction for history
            var tx = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
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
        public async Task<ActionResult> Withdraw(Guid userId, decimal amount)
        {
            if (amount <= 0) return BadRequest("Amount must be positive");
            
            var wallet = await GetOrCreateWallet(userId);
            if (wallet.Balance < amount) return BadRequest("Insufficient balance");

            wallet.Balance -= amount;
            wallet.TotalWithdrawn += amount;
            wallet.LastUpdated = DateTime.UtcNow;

            // Log as transaction for history
            var tx = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
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
