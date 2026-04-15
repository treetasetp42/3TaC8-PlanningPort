using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _3TaC8_PlanningPort.Entities
{
    public class CashWallet
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PortfolioId { get; set; }
        public Portfolio? Portfolio { get; set; }

        [Column(TypeName = "decimal(18, 4)")]
        public decimal Balance { get; set; } = 0;

        [Column(TypeName = "decimal(18, 4)")]
        public decimal TotalDeposited { get; set; } = 0;

        [Column(TypeName = "decimal(18, 4)")]
        public decimal TotalWithdrawn { get; set; } = 0;

        [Column(TypeName = "decimal(18, 4)")]
        public decimal TotalRealizedProfit { get; set; } = 0;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
