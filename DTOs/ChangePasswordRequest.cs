using System.ComponentModel.DataAnnotations;

namespace _3TaC8_PlanningPort.DTOs
{
    public class ChangePasswordRequest
    {
        [MaxLength(128)]
        public string CurrentPassword { get; set; } = string.Empty;
        [Required, StringLength(128, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;
        [Required, StringLength(128, MinimumLength = 8)]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
