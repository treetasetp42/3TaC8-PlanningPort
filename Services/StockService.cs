using _3TaC8_PlanningPort.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;

namespace _3TaC8_PlanningPort.Services
{
    public class StockService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;

        public StockService(HttpClient httpClient, IConfiguration config, ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _config = config;
            _context = context;  
        }
        public async Task<decimal> GetCurrentPriceAsync(string symbol)
        {
            var apiKey = _config["Finnhub:ApiKey"];
            var url = $"https://finnhub.io/api/v1/quote?symbol={symbol.ToUpper()}&token={apiKey}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var json = JsonDocument.Parse(content);

                // 'c' ใน Finnhub คือ Current Price (ราคาปัจจุบัน)
                if (json.RootElement.TryGetProperty("c", out var priceElement))
                {
                    return priceElement.GetDecimal();
                }
            }
            catch (Exception ex)
            {
                // ใน V.1 เราส่ง 0 กลับไปก่อนถ้าดึงข้อมูลไม่ได้ (เช่น พิมพ์ชื่อหุ้นผิด)
                Console.WriteLine($"Error fetching price for {symbol}: {ex.Message}");
            }

            return 0;
        }
        public async Task<decimal> GetPriceWithCacheAsync(string symbol)
        {
            var marketPrice = await GetCurrentPriceAsync(symbol); // ดึงจาก API [cite: 2026-04-01]

            if (marketPrice > 0)
            {
                // อัปเดตลง Cache ใน DB [cite: 2026-04-01]
                var cache = await _context.StockCaches.FindAsync(symbol.ToUpper());
                if (cache == null)
                {
                    _context.StockCaches.Add(new StockCache { Symbol = symbol.ToUpper(), LastPrice = marketPrice });
                }
                else
                {
                    cache.LastPrice = marketPrice;
                    cache.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
                return marketPrice;
            }

            // ถ้า API ล่ม/ออฟไลน์ ให้ไปดึงจาก Cache [cite: 2026-04-01]
            var savedCache = await _context.StockCaches.FindAsync(symbol.ToUpper());
            return savedCache?.LastPrice ?? 0;
        }
    }
}