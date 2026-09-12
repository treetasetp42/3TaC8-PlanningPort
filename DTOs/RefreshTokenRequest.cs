using System.ComponentModel.DataAnnotations;

namespace _3TaC8_PlanningPort.DTOs
{
    public class RefreshTokenRequest
    {
        [Required, StringLength(512, MinimumLength = 20)]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
