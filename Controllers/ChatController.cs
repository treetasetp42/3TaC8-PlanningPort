using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using System.Text.Json;

namespace _3TaC8_PlanningPort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ChatController> logger) : ControllerBase
    {
        // สั่งอ่านค่าจาก configuration มาเก็บไว้ที่ Field ได้เลยโดยไม่ต้องมีฟังก์ชัน Constructor
        private readonly string _apiKey = 
            configuration["Gemini:ApiKey"] ?? 
            configuration["GEMINI_API_KEY"] ?? 
            throw new ArgumentNullException("Gemini API Key is missing. Please set 'Gemini:ApiKey' or environment variable 'GEMINI_API_KEY'.");

        [HttpPost]
        [EnableRateLimiting("expensive")]
        public async Task<IActionResult> AskBot([FromBody] ChatRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Message cannot be empty");
            if (request.Message.Length > 1_000 || request.CurrentPath?.Length > 200)
                return BadRequest("Message or path is too long.");

            // 1. กำหนด System Instruction เพื่อตีกรอบให้บอทตอบเฉพาะเรื่องในเว็บ และใช้น้ำเสียงผู้หญิง (ค่ะ/นะคะ)
            var systemInstructionBuilder = new StringBuilder();
            systemInstructionBuilder.Append("คุณคือผู้ช่วยส่วนตัวผู้หญิงสุดสุภาพของเว็บไซต์ InvestPlanner (โปรเจกต์จำลองการจัดการพอร์ตลงทุน) ");
            systemInstructionBuilder.Append("หน้าที่ของคุณคือตอบคำถามเกี่ยวกับการใช้งานเว็บ การคำนวณเงินสด และข้อมูลหุ้นเท่านั้น ");
            systemInstructionBuilder.Append("กรุณาตอบคำถามอย่างสุภาพและเป็นมิตรด้วยน้ำเสียงของผู้หญิง โดยใช้คำลงท้ายว่า 'ค่ะ' หรือ 'นะคะ' เสมอ (ห้ามใช้คำลงท้ายของผู้ชาย เช่น 'ครับ' หรือแบบผสม 'ครับ/ค่ะ' เด็ดขาด) ");
            systemInstructionBuilder.Append("หากผู้ใช้ถามเรื่องอื่นที่ไม่เกี่ยวข้อง ให้ปฏิเสธอย่างสุภาพและแจ้งว่าสามารถตอบได้เฉพาะเรื่องของ InvestPlanner เท่านั้น ");

            if (!string.IsNullOrWhiteSpace(request.CurrentPath))
            {
                systemInstructionBuilder.Append($"ขณะนี้ผู้ใช้กำลังดูหน้าเว็บที่เส้นทาง (Path): '{request.CurrentPath}' ");
                systemInstructionBuilder.Append("กรุณาใช้ข้อมูลของหน้าเว็บปัจจุบันนี้เพื่อทำความเข้าใจคำถาม แนะนำปุ่ม เมนู หรืออธิบายข้อมูลบริบทให้เหมาะสมกับหน้าเว็บดังกล่าวมากที่สุด ");
            }

            string systemInstruction = systemInstructionBuilder.ToString();

            // 2. จัดโครงสร้าง Request Body ให้ตรงตามที่ Gemini API กำหนด  
            try
            {
                var geminiRequest = new
                {
                    system_instruction = new { parts = new[] { new { text = systemInstruction } } },
                    contents = new[] { new { role = "user", parts = new[] { new { text = request.Message } } } }
                };

                var jsonPayload = JsonSerializer.Serialize(geminiRequest);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // เปลี่ยนมาใช้ gemini-2.5-flash เพื่อความเสถียรและหลีกเลี่ยงข้อจำกัดการใช้งานหนาแน่นของรุ่น Lite
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
                var response = await httpClientFactory.CreateClient("Gemini").PostAsync(url, content, cancellationToken);

                // ดึงข้อความดิบที่ Google ตอบกลับมาก่อน
                var jsonString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Gemini returned status {StatusCode}.", response.StatusCode);
                    return StatusCode(StatusCodes.Status502BadGateway, "The AI service is temporarily unavailable.");
                }

                // 4. แกะ JSON เอาเฉพาะข้อความตอบกลับของ AI ออกมา
                using var doc = JsonDocument.Parse(jsonString);
                var botResponse = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return Ok(new { response = botResponse });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Gemini request failed.");
                return StatusCode(StatusCodes.Status502BadGateway, "The AI service is temporarily unavailable.");
            }
        }
    }

    // DTO สำหรับรับข้อมูลจาก React
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? CurrentPath { get; set; }
    }
}
