using _3TaC8_PlanningPort.Entities; 
using Microsoft.EntityFrameworkCore;

namespace _3TaC8_PlanningPort.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Watchlist> Watchlists { get; set; }
        public DbSet<StockCache> StockCaches { get; set; }
        public DbSet<UserLog> UserLogs { get; set; }
        public DbSet<CashWallet> CashWallets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CashWallet>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique(); // One wallet per user
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RemoteUser).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.RemoteUser).IsUnique(); 
            });
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);

                // เชื่อม Transaction หลายรายการเข้ากับ User 1 คน
                entity.HasOne(d => d.User)
                      .WithMany()
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade); // ถ้าลบ User ให้ลบ Transaction ของเขาด้วย

                entity.Property(e => e.Symbol).IsRequired();
            });
            modelBuilder.Entity<UserLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired();
                // ให้ SQL Server ใส่เวลาปัจจุบันให้อัตโนมัติถ้าเราไม่ได้ส่งไป [cite: 2026-04-02]
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            });
            modelBuilder.Entity<StockCache>(entity =>
            {
                // Composite primary key: Exchange + Symbol prevents collisions (e.g. NASDAQ:AAPL vs NYSE:AAPL)
                entity.HasKey(e => new { e.Symbol, e.Exchange });
            });
        }
    }
}