namespace _3TaC8_PlanningPort.Entities
{
    public class UserOAuth
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        
        public string ProviderName { get; set; } = string.Empty; // e.g., 'Google'
        public string ProviderKey { get; set; } = string.Empty; // Unique ID from provider (Sub in Google)

        // Navigation property back to User
        public virtual User User { get; set; } = null!;
    }
}
