using _3TaC8_PlanningPort.Data;
using _3TaC8_PlanningPort.Entities;
using Microsoft.EntityFrameworkCore;

namespace _3TaC8_PlanningPort.Data
{
    public static class PermissionSeeder
    {
        // ── All permission keys defined in code (source of truth) ──────────────
        private static readonly List<(string Key, string Description, string Module)> AllPermissions = new()
        {
            // Admin Module
            ("ADMIN_ACCESS",         "Access the Admin Dashboard",          "Admin"),
            ("ADMIN_USERS_VIEW",     "View user list and details",          "Admin"),
            ("ADMIN_USERS_EDIT",     "Edit, ban, and reset user accounts",  "Admin"),
            ("ADMIN_ROLES_VIEW",     "View roles and their permissions",    "Admin"),
            ("ADMIN_ROLES_MANAGE",   "Create and manage role permissions",  "Admin"),

            // Portfolio Module
            ("PORTFOLIO_VIEW",       "View own portfolios",                 "Portfolio"),
            ("PORTFOLIO_CREATE",     "Create new portfolios",               "Portfolio"),
            ("PORTFOLIO_DELETE",     "Delete portfolios",                   "Portfolio"),

            // Market Module
            ("MARKET_VIEW",          "View the Market page",                "Market"),
            ("MARKET_TRADE",         "Execute Buy/Sell transactions",       "Market"),
        };

        // ── Default role definitions ────────────────────────────────────────────
        private static readonly List<(string Name, string Description, string[] Permissions)> DefaultRoles = new()
        {
            ("Member", "Standard user with basic access", new[]
            {
                "PORTFOLIO_VIEW", "PORTFOLIO_CREATE", "PORTFOLIO_DELETE",
                "MARKET_VIEW", "MARKET_TRADE"
            }),
            ("Pro", "Pro user with all portfolio and market features", new[]
            {
                "PORTFOLIO_VIEW", "PORTFOLIO_CREATE", "PORTFOLIO_DELETE",
                "MARKET_VIEW", "MARKET_TRADE"
            }),
            ("Moderator", "Can view admin area and user list", new[]
            {
                "PORTFOLIO_VIEW", "PORTFOLIO_CREATE", "PORTFOLIO_DELETE",
                "MARKET_VIEW", "MARKET_TRADE",
                "ADMIN_ACCESS", "ADMIN_USERS_VIEW"
            }),
            ("Admin", "Full system access", new[]
            {
                "ADMIN_ACCESS", "ADMIN_USERS_VIEW", "ADMIN_USERS_EDIT",
                "ADMIN_ROLES_VIEW", "ADMIN_ROLES_MANAGE",
                "PORTFOLIO_VIEW", "PORTFOLIO_CREATE", "PORTFOLIO_DELETE",
                "MARKET_VIEW", "MARKET_TRADE"
            }),
        };

        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // 0. Data cleanup: convert existing empty strings to NULLs to satisfy unique constraint
            await context.Database.ExecuteSqlRawAsync("UPDATE Users SET Email = NULL WHERE Email = ''");

            // 1. Seed Permissions (upsert by key)
            foreach (var (key, description, module) in AllPermissions)
            {
                if (!await context.Permissions.AnyAsync(p => p.Key == key))
                {
                    context.Permissions.Add(new Permission
                    {
                        Key = key,
                        Description = description,
                        Module = module
                    });
                }
            }
            await context.SaveChangesAsync();

            // 2. Seed Roles and their permissions
            foreach (var (name, description, permissions) in DefaultRoles)
            {
                var role = await context.Roles
                    .Include(r => r.RolePermissions)
                    .FirstOrDefaultAsync(r => r.Name == name);

                if (role == null)
                {
                    role = new Role { Name = name, Description = description };
                    context.Roles.Add(role);
                    await context.SaveChangesAsync();
                }

                // Add missing permissions to this role
                foreach (var permKey in permissions)
                {
                    if (!role.RolePermissions.Any(rp => rp.PermissionKey == permKey))
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionKey = permKey
                        });
                    }
                }
            }
            await context.SaveChangesAsync();

            // 3. Migrate existing users: map old string Role → new RoleId
            //    Only needed once for users created before this migration
            var memberRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Member");
            var adminRole  = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var proRole    = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Pro");
            var modRole    = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Moderator");

            // Users whose RoleId is still the default (1) but haven't been explicitly set
            // are already fine (Member = Id 1 if seeded in order). No action needed for them.

            // 4. Auto-promote the first registered user to Admin (if they're still Member)
            var firstUser = await context.Users
                .OrderBy(u => u.CreatedAt)
                .FirstOrDefaultAsync();

            if (firstUser != null && adminRole != null && firstUser.RoleId == (memberRole?.Id ?? 1))
            {
                firstUser.RoleId = adminRole.Id;
                await context.SaveChangesAsync();
                Console.WriteLine($"[Seeder] Promoted first user '{firstUser.RemoteUser}' to Admin.");
            }
        }
    }
}
