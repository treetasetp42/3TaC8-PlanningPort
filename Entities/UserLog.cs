namespace _3TaC8_PlanningPort.Entities
{
    public class UserLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Action { get; set; } = string.Empty; 
        public string IPAddress { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
