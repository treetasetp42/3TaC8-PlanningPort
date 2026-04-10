namespace _3TaC8_PlanningPort.DTOs
{
    public class UpdateProfileRequest
    {
        public string? RemoteUser { get; set; }
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public IFormFile? AvatarFile { get; set; }
        public bool DeleteCurrentAvatar { get; set; } = false;
    }
}
