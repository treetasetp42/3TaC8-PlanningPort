using _3TaC8_PlanningPort.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Text.Json;

namespace _3TaC8_PlanningPort.Services
{
    public class StockPriceResponse
    {
        public decimal CurrentPrice { get; set; }
        public decimal Change { get; set; }
        public decimal PercentChange { get; set; }
    }

    public class StockService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;

        public StockService(HttpClient httpClient, IConfiguration config, ApplicationDbContext context, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _config = config;
            _context = context;  
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Parse a TradingView full symbol string (e.g. "NASDAQ:AAPL" or "AAPL") into (exchange, symbol).
        /// Defaults exchange to "NASDAQ" if no prefix is found.
        /// </summary>
        public static (string exchange, string symbol) ParseTvSymbol(string fullSymbol)
        {
            var upper = fullSymbol.ToUpper().Trim();
            if (upper.Contains(':'))
            {
                var parts = upper.Split(':', 2);
                return (parts[0], parts[1]);
            }
            return ("NASDAQ", upper);
        }

        /// <summary>
        /// Build the correct Finnhub query symbol based on the exchange.
        /// Finnhub uses plain ticker for US stocks and "EXCHANGE:TICKER" for crypto.
        /// </summary>
        private static string BuildFinnhubSymbol(string exchange, string symbol)
        {
            return exchange == "BINANCE" ? $"BINANCE:{symbol}" : symbol;
        }

        public async Task<StockPriceResponse> GetCurrentPriceAsync(string symbol, string exchange = "NASDAQ")
        {
            if (string.IsNullOrWhiteSpace(symbol) || symbol.Length > 20 ||
                string.IsNullOrWhiteSpace(exchange) || exchange.Length > 20)
                return new StockPriceResponse();

            var apiKey = _config["Finnhub:ApiKey"];
            var finnhubSymbol = BuildFinnhubSymbol(exchange, symbol.ToUpper());
            var memoryKey = $"stock:{exchange.ToUpperInvariant()}:{symbol.ToUpperInvariant()}";
            if (_memoryCache.TryGetValue(memoryKey, out StockPriceResponse? memoryValue) && memoryValue != null)
                return memoryValue;

            var url = $"https://finnhub.io/api/v1/quote?symbol={Uri.EscapeDataString(finnhubSymbol)}&token={Uri.EscapeDataString(apiKey ?? string.Empty)}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var json = JsonDocument.Parse(content);

                // 'c' = Current Price, 'd' = Change, 'dp' = Percent Change
                if (json.RootElement.TryGetProperty("c", out var priceElement))
                {
                    var result = new StockPriceResponse
                    {
                        CurrentPrice = priceElement.GetDecimal(),
                        Change = json.RootElement.TryGetProperty("d", out var change) ? change.GetDecimal() : 0,
                        PercentChange = json.RootElement.TryGetProperty("dp", out var pc) ? pc.GetDecimal() : 0
                    };
                    _memoryCache.Set(memoryKey, result, TimeSpan.FromMinutes(result.CurrentPrice > 0 ? 1 : 5));
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching price for {exchange}:{symbol}: {ex.Message}");
            }

            var unavailable = new StockPriceResponse { CurrentPrice = 0, Change = 0, PercentChange = 0 };
            _memoryCache.Set(memoryKey, unavailable, TimeSpan.FromMinutes(1));
            return unavailable;
        }

        public async Task<StockPriceResponse> GetPriceWithSnapshotAsync(string symbol, string exchange = "NASDAQ")
        {
            var upperSymbol = symbol.ToUpper();
            var upperExchange = exchange.ToUpper();

            // 1. Check cache first (5-minute window) [cite: 2026-04-02]
            var cachedData = await _context.StockCaches
                .FirstOrDefaultAsync(c => c.Symbol == upperSymbol && c.Exchange == upperExchange);

            if (cachedData != null && (DateTime.UtcNow - cachedData.UpdatedAt).TotalMinutes < 5)
            {
                return new StockPriceResponse 
                { 
                    CurrentPrice = cachedData.LastPrice,
                    Change = cachedData.DailyChange,
                    PercentChange = cachedData.DailyPercentChange
                };
            }

            // 2. Fetch fresh price from Finnhub
            var marketData = await GetCurrentPriceAsync(upperSymbol, upperExchange);

            if (marketData.CurrentPrice > 0)
            {
                await UpdateStockCache(upperSymbol, upperExchange, marketData);
                return marketData;
            }

            // 3. Fallback: return stale cache if API fails
            return new StockPriceResponse
            {
                CurrentPrice = cachedData?.LastPrice ?? 0,
                Change = cachedData?.DailyChange ?? 0,
                PercentChange = cachedData?.DailyPercentChange ?? 0
            };
        }

        private async Task UpdateStockCache(string symbol, string exchange, StockPriceResponse data)
        {
            var cache = await _context.StockCaches
                .FirstOrDefaultAsync(c => c.Symbol == symbol && c.Exchange == exchange);

            if (cache == null)
            {
                _context.StockCaches.Add(new StockCache
                {
                    Symbol = symbol,
                    Exchange = exchange,
                    LastPrice = data.CurrentPrice,
                    DailyChange = data.Change,
                    DailyPercentChange = data.PercentChange,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                cache.LastPrice = data.CurrentPrice;
                cache.DailyChange = data.Change;
                cache.DailyPercentChange = data.PercentChange;
                cache.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }
    }
 
}
