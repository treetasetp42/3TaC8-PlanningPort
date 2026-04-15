using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _3TaC8_PlanningPort.Entities
{
    public class Portfolio
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        [MaxLength(20)]
        public string ColorCode { get; set; } = "#6C5DD3"; // Default primary color

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual CashWallet? CashWallet { get; set; }
    }
}
