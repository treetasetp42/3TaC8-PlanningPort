using System.ComponentModel.DataAnnotations;

namespace _3TaC8_PlanningPort.DTOs
{
    public class CreateUserRequest
    {
        [Required, StringLength(64, MinimumLength = 3)]
        [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Username contains unsupported characters.")]
        public string RemoteUser { get; set; } = string.Empty;
        [Required, StringLength(128, MinimumLength = 8)]
        public string RemotePassword { get; set; } = string.Empty;
        [EmailAddress, MaxLength(255)]
        public string? Email { get; set; }
    }
}
