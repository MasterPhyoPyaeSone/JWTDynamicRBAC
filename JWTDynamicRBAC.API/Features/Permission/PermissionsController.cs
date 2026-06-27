
using JWTDynamicRBAC.API.Features.Filters;
using JWTDynamicRBAC.API.Features.Permission;
using JWTDynamicRBAC.Database.AppDbContextModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


[Route("api/[controller]")]
[ApiController]
public class PermissionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PermissionsController(AppDbContext context)
    {
        _context = context;
    }

    // 1. READ ALL (Get all permissions)
    // GET: api/permissions
    [HttpGet]
    [Permission ("View_Permissions")]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await _context.Permissions
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                PermissionName = p.PermissionName
            })
            .ToListAsync();

        return Ok(permissions);
    }

    // 2. READ ONE (Get a specific permission by ID)
    // GET: api/permissions/5
    [HttpGet("{id}")]
    [Permission ("View_Permissions")]
    public async Task<IActionResult> GetPermission(int id)
    {
        var permission = await _context.Permissions.FindAsync(id);

        if (permission == null)
        {
            return NotFound("Permission not found.");
        }

        var dto = new PermissionDto
        {
            Id = permission.Id,
            PermissionName = permission.PermissionName
        };

        return Ok(dto);
    }

    // 3. CREATE (Add a new permission)
    // POST: api/permissions
    [HttpPost]
    [Permission ("View_Permissions")]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PermissionName))
        {
            return BadRequest("Permission name is required.");
        }

        // Check if permission already exists to avoid duplicates
        var exists = await _context.Permissions.AnyAsync(p => p.PermissionName == dto.PermissionName);
        if (exists)
        {
            return BadRequest("Permission already exists.");
        }

        var permission = new Permission
        {
            PermissionName = dto.PermissionName
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        var returnDto = new PermissionDto
        {
            Id = permission.Id,
            PermissionName = permission.PermissionName
        };

        // Return a 201 Created response
        return CreatedAtAction(nameof(GetPermission), new { id = permission.Id }, returnDto);
    }

    // 4. UPDATE (Modify an existing permission)
    // PUT: api/permissions/5
    [HttpPut("{id}")]
    [Permission ("View_Permissions")]
    public async Task<IActionResult> UpdatePermission(int id, [FromBody] UpdatePermissionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PermissionName))
        {
            return BadRequest("Permission name is required.");
        }

        var permission = await _context.Permissions.FindAsync(id);

        if (permission == null)
        {
            return NotFound("Permission not found.");
        }

        // Check if the new name already exists on another record
        var exists = await _context.Permissions.AnyAsync(p => p.PermissionName == dto.PermissionName && p.Id != id);
        if (exists)
        {
             return BadRequest("A permission with this name already exists.");
        }

        permission.PermissionName = dto.PermissionName;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PermissionExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent(); // 204 No Content is standard for a successful PUT
    }

    // 5. DELETE (Remove a permission)
    // DELETE: api/permissions/5
    [HttpDelete("{id}")]
    [Permission ("View_Permissions")]
    public async Task<IActionResult> DeletePermission(int id)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission == null)
        {
            return NotFound("Permission not found.");
        }

        _context.Permissions.Remove(permission);
        await _context.SaveChangesAsync();

        return NoContent(); // 204 No Content
    }

    // Helper method to check if a permission exists
    private bool PermissionExists(int id)
    {
        return _context.Permissions.Any(e => e.Id == id);
    }
}