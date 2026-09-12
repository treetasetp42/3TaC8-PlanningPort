using System.ComponentModel.DataAnnotations;

namespace _3TaC8_PlanningPort.DTOs
{
    public class TransactionRequest
    {
        [Required, StringLength(20, MinimumLength = 1)]
        [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Symbol contains unsupported characters.")]
        public string Symbol { get; set; } = string.Empty;
        [Required, RegularExpression("^(Buy|Sell)$")]
        public string Type { get; set; } = "Buy"; // Buy or Sell
        [Range(typeof(decimal), "0.00000001", "10000000")]
        public decimal Quantity { get; set; }
        [Range(typeof(decimal), "0.00000001", "10000000")]
        public decimal PricePerUnit { get; set; }
        [Required, StringLength(10, MinimumLength = 3)]
        public string Currency { get; set; } = "USD";
        [Required, MaxLength(50)]
        public string AssetType { get; set; } = "Stock";
        [MaxLength(50)]
        public string? Subtype { get; set; }
        [Required, StringLength(20, MinimumLength = 1)]
        [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Exchange contains unsupported characters.")]
        public string Exchange { get; set; } = "NASDAQ";
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    }
}
