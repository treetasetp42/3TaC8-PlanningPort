using _3TaC8_PlanningPort.Entities; 
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace _3TaC8_PlanningPort.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserOAuth> UserOAuths { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Watchlist> Watchlists { get; set; }
        public DbSet<StockCache> StockCaches { get; set; }
        public DbSet<UserLog> UserLogs { get; set; }
        public DbSet<CashWallet> CashWallets { get; set; }
        public DbSet<Portfolio> Portfolios { get; set; }

        // RBAC
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserPenalty> UserPenalties { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CashWallet>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PortfolioId).IsUnique();
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RemoteUser).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.RemoteUser).IsUnique(); 
                entity.HasIndex(e => e.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

                // FK to Role
                entity.HasOne(e => e.Role)
                      .WithMany()
                      .HasForeignKey(e => e.RoleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserOAuth>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProviderName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ProviderKey).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => new { e.ProviderName, e.ProviderKey }).IsUnique();

                entity.HasOne(d => d.User)
                      .WithMany(p => p.OAuthProviders)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Token).IsUnique(); 

                entity.HasOne(d => d.User)
                      .WithMany(p => p.RefreshTokens)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(d => d.Portfolio)
                      .WithMany(p => p.Transactions)
                      .HasForeignKey(d => d.PortfolioId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.Symbol).IsRequired();
            });

            modelBuilder.Entity<UserLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired();
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<Portfolio>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(d => d.User)
                      .WithMany(p => p.Portfolios)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StockCache>(entity =>
            {
                entity.HasKey(e => new { e.Symbol, e.Exchange });
                entity.Property(e => e.LastPrice).HasColumnType("decimal(18,4)");
                entity.Property(e => e.DailyChange).HasColumnType("decimal(18,4)");
                entity.Property(e => e.DailyPercentChange).HasColumnType("decimal(18,4)");
            });

            modelBuilder.Entity<UserPenalty>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.User)
                      .WithMany(u => u.Penalties)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Admin)
                      .WithMany()
                      .HasForeignKey(e => e.AdminId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── RBAC ──────────────────────────────────────────────────────────

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.Key);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Module).HasMaxLength(50);
            });

            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(e => new { e.RoleId, e.PermissionKey });

                entity.HasOne(e => e.Role)
                      .WithMany(r => r.RolePermissions)
                      .HasForeignKey(e => e.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Permission)
                      .WithMany(p => p.RolePermissions)
                      .HasForeignKey(e => e.PermissionKey)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}