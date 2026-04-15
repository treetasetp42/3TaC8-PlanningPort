using System;

namespace _3TaC8_PlanningPort.Entities
{
    public class UserPenalty
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public virtual User? User { get; set; }

        public Guid? AdminId { get; set; }
        public virtual User? Admin { get; set; }

        public string Action { get; set; } = string.Empty; // Ban, Unban, Disable, Enable, PasswordReset
        public string? Reason { get; set; }
        public string? Details { get; set; } // Duration, system notes, etc.
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
