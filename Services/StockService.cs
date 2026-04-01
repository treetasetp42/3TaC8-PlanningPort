using System.Net.Http;
using System.Text.Json;

namespace _3TaC8_PlanningPort.Services
{
    public class StockService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public StockService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
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
    }
}