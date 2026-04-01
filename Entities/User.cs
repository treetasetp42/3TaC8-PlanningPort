namespace _3TaC8_PlanningPort.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string RemoteUser { get; set; } = string.Empty;
        public string? RemotePassword { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}