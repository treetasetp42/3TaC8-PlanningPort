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

        // FK to Role table — replaces the old string Role field
        public int RoleId { get; set; } = 1; // default: Member (Id=1 after seeding)
        public virtual Role? Role { get; set; }

        public DateTime? BannedUntil { get; set; }
        public string? BanReason { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeleteRequestedAt { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }

        // Navigation properties
        public virtual ICollection<UserOAuth> OAuthProviders { get; set; } = new Collection<UserOAuth>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new Collection<RefreshToken>();
        public virtual ICollection<Portfolio> Portfolios { get; set; } = new Collection<Portfolio>();
        public virtual ICollection<UserPenalty> Penalties { get; set; } = new Collection<UserPenalty>();
    }
}