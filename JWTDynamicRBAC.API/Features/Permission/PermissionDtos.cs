// For creating a new permission
namespace JWTDynamicRBAC.API.Features.Permission;
public class CreatePermissionDto
{
    public string PermissionName { get; set; } = string.Empty;
}

// For updating an existing permission
public class UpdatePermissionDto
{
    public string PermissionName { get; set; } = string.Empty;
}

// For returning permission data
public class PermissionDto
{
    public int Id { get; set; }
    public string PermissionName { get; set; } = string.Empty;
}