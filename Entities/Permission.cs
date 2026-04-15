namespace _3TaC8_PlanningPort.Entities
{
    public class Permission
    {
        public string Key { get; set; } = string.Empty;   // e.g. "ADMIN_USERS_VIEW"
        public string Description { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty; // e.g. "Admin", "Portfolio"

        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
