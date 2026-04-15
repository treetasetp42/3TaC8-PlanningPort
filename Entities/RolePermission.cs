namespace _3TaC8_PlanningPort.Entities
{
    public class RolePermission
    {
        public int RoleId { get; set; }
        public string PermissionKey { get; set; } = string.Empty;

        public virtual Role Role { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }
}
