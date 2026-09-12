using System.ComponentModel.DataAnnotations;

namespace _3TaC8_PlanningPort.DTOs
{
    public class UpdateProfileRequest
    {
        [StringLength(64, MinimumLength = 3)]
        [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Username contains unsupported characters.")]
        public string? RemoteUser { get; set; }
        [MaxLength(100)]
        public string? DisplayName { get; set; }
        [EmailAddress, MaxLength(255)]
        public string? Email { get; set; }
        [Url, MaxLength(2048)]
        public string? AvatarUrl { get; set; }
        public IFormFile? AvatarFile { get; set; }
        public bool DeleteCurrentAvatar { get; set; } = false;
    }
}
