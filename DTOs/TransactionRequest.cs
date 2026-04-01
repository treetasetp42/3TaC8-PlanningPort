namespace _3TaC8_PlanningPort.DTOs
{
    public class TransactionRequest
    {
        public string Symbol { get; set; } = string.Empty;
        public string Type { get; set; } = "Buy"; // Buy or Sell
        public decimal Quantity { get; set; }
        public decimal PricePerUnit { get; set; }
        public string Currency { get; set; } = "USD";
        public string AssetType { get; set; } = "Stock";
        public string? Subtype { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    }
}