using JWTDynamicRBAC.API.Features.Filters;
using JWTDynamicRBAC.Database.AppDbContextModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _context;

    public RolesController(AppDbContext context)
    {
        _context = context;
    }

    // ၁။ Role အားလုံးကို ယူရန် (Dropdown အတွက်)
    [HttpGet]
    [Permission("View_RolePermissions")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _context.Roles
            .Select(r => new { r.Id, r.RoleName })
            .ToListAsync();
        return Ok(roles);
    }

    // ၂။ စနစ်ထဲမှာရှိတဲ့ Permission အားလုံးကို ယူရန်
    [HttpGet("permissions")]
    // [Permission("View_RolePermissions")]
    public async Task<IActionResult> GetAllPermissions()
    {
        var permissions = await _context.Permissions
            .Select(p => new { p.Id, p.PermissionName })
            .ToListAsync();
        return Ok(permissions);
    }

    // ၃။ သတ်မှတ်ထားသော Role ရဲ့ လက်ရှိ Permission ID များကို ယူရန်
    [HttpGet("{roleId}/permissions")]
    [Permission("View_RolePermissions")]
    public async Task<IActionResult> GetRolePermissions(int roleId)
    {
        var role = await _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null) return NotFound("Role not found.");

        var permissionIds = role.Permissions.Select(p => p.Id).ToList();
        return Ok(permissionIds);
    }

    // ၄။ Role ကို Permission အသစ်များ Assign လုပ်ရန်
    [HttpPost("{roleId}/permissions")]
    [Permission("View_RolePermissions")]
    public async Task<IActionResult> AssignPermissions(int roleId, [FromBody] List<int> selectedPermissionIds)
    {
        var role = await _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null) return NotFound("Role not found.");

        // User ရွေးချယ်ထားသော Permission များကို Database မှ ရှာဖွေခြင်း
        var newPermissions = await _context.Permissions
            .Where(p => selectedPermissionIds.Contains(p.Id))
            .ToListAsync();

        // လက်ရှိ Permission များကို ရှင်းထုတ်ပြီး အသစ်များဖြင့် အစားထိုးခြင်း
        role.Permissions.Clear();
        foreach (var permission in newPermissions)
        {
            role.Permissions.Add(permission);
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Permissions updated successfully." });
    }
}