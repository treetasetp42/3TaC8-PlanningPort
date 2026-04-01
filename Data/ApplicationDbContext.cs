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
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
        }
    }
}