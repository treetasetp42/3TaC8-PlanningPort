using System.ComponentModel.DataAnnotations;

public class StockCache
{
    [Key]
    public string Symbol { get; set; } = string.Empty; // ใช้ Symbol เป็น Key เลย
    public decimal LastPrice { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}