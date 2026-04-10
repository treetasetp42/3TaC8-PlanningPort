using System.Collections.ObjectModel;

namespace _3TaC8_PlanningPort.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string RemoteUser { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? RemotePassword { get; set; }
        public string Role { get; set; } = "Member";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeleteRequestedAt { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }

        // Navigation property for 1-to-Many OAuth providers
        public virtual ICollection<UserOAuth> OAuthProviders { get; set; } = new Collection<UserOAuth>();
        // Navigation property for 1-to-Many Refresh tokens
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new Collection<RefreshToken>();
    }
}