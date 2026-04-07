using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class StockCache
{
    [Key, Column(Order = 0)]
    public string Symbol { get; set; } = string.Empty; // Ticker only, e.g. AAPL
    [Key, Column(Order = 1)]
    public string Exchange { get; set; } = "NASDAQ";   // e.g. NASDAQ, NYSE, BINANCE
    public decimal LastPrice { get; set; }
    public decimal DailyChange { get; set; }           // d
    public decimal DailyPercentChange { get; set; }    // dp
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}