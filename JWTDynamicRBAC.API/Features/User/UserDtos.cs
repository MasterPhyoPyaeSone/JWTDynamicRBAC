namespace JWTDynamicRBAC.API.Features.User;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
}

public class CreateUserDto
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public int? RoleId { get; set; }
}

public class UpdateUserDto
{
    public string Username { get; set; } = null!;
    public int? RoleId { get; set; }
}

// Role နဲ့ Permission DTOs (ယခင်ပေးထားတဲ့အတိုင်း)
public class RoleDto { public int Id { get; set; } public string RoleName { get; set; } = null!; }
public class PermissionDto { public int Id { get; set; } public string PermissionName { get; set; } = null!; }