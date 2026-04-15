using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _3TaC8_PlanningPort.Entities
{
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // เชื่อมกับ Portfolio (Foreign Key)
        [Required]
        public Guid PortfolioId { get; set; }
        public Portfolio? Portfolio { get; set; }

        [Required]
        [MaxLength(20)]
        public string Symbol { get; set; } = string.Empty; // เช่น NVDA, BTC

        [Required]
        public string Type { get; set; } = "Buy"; // 'Buy' หรือ 'Sell'

        [Required]
        [Column(TypeName = "decimal(18, 4)")]
        public decimal Quantity { get; set; } // จำนวนหุ้น

        [Required]
        [Column(TypeName = "decimal(18, 4)")]
        public decimal PricePerUnit { get; set; } // ราคาต่อหน่วย

        [Required]
        public string Currency { get; set; } = "USD"; // 'USD' หรือ 'THB'

        // สำหรับข้อ 6: Dashboard & Filtering
        public string AssetType { get; set; } = "Stock"; // เช่น Stock, Crypto, Index
        public string? Subtype { get; set; } // เช่น Tech, S&P500, US, TH
        public string Exchange { get; set; } = "NASDAQ"; // เช่น NASDAQ, BINANCE

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}