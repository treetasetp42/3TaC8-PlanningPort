using _3TaC8_PlanningPort.Entities;

public class Watchlist
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Exchange { get; set; } = "NASDAQ"; // e.g. NASDAQ, NYSE, BINANCE
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}