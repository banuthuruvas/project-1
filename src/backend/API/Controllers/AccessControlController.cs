using System.Text.Json;
using API.Authorization;
using Domain.Dto;
using Domain.Enum;
using Domain.Security;
using Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Manages access functions, roles, and user-role assignments.
/// Access is modeled through screen-level and API-level access functions.
/// </summary>
public class AccessControlController : BaseController
{
    private readonly IUserRoleService _userRoleService;
    private readonly IAccessFunctionService _accessFunctionService;
    private readonly IRoleService _roleService;
    private readonly IAuditLogger _auditLogger;

    public AccessControlController(
        IUserRoleService userRoleService,
        IAccessFunctionService accessFunctionService,
        IRoleService roleService,
        IAuditLogger auditLogger)
    {
        _userRoleService = userRoleService;
        _accessFunctionService = accessFunctionService;
        _roleService = roleService;
        _auditLogger = auditLogger;
    }

    /// <summary>
    /// Returns the complete access-control snapshot used by the administration screen.
    /// </summary>
    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlRead)]
    public async Task<ActionResult<AccessControlOverviewDto>> GetOverview()
    {
        var overview = new AccessControlOverviewDto
        {
            Users = await _userRoleService.GetAccessControlUsersAsync(),
            Roles = await _roleService.GetAllAsync(),
            AccessFunctions = await _accessFunctionService.GetAllAsync()
        };

        return Ok(overview);
    }

    /// <summary>
    /// Returns the current user's role and access profile for screen-level checks.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CurrentAccessProfileDto>> GetCurrentAccessProfile()
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        var userRoles = await _userRoleService.GetUserRolesAsync(UserId);
        var accessFunctionCodes = await _accessFunctionService.GetUserAccessFunctionCodesAsync(UserId);

        return Ok(new CurrentAccessProfileDto
        {
            UserId = UserId,
            RoleCodes = userRoles
                .Where(role => role.IsActive)
                .Select(role => role.RoleCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code)
                .ToList(),
            RoleNames = userRoles
                .Where(role => role.IsActive)
                .Select(role => role.RoleName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList(),
            AccessFunctionCodes = accessFunctionCodes
        });
    }

    /// <summary>
    /// Creates a new role with its granted access functions.
    /// </summary>
    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlRolesManage)]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleDto dto)
    {
        var role = await _roleService.CreateAsync(dto);

        await _auditLogger.LogAsync(
            EAuditAction.RoleCreated,
            EAuditCategory.AccessControl,
            "Role",
            role.Id.ToString(),
            newValues: JsonSerializer.Serialize(role));

        await _auditLogger.LogRoleAccessChangedAsync(
            role.Code,
            role.AccessFunctions.Select(accessFunction => accessFunction.Code),
            newValues: JsonSerializer.Serialize(role.AccessFunctions.Select(accessFunction => accessFunction.Code)));

        return Ok(role);
    }

    /// <summary>
    /// Updates an existing role and its granted access functions.
    /// </summary>
    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlRolesManage)]
    public async Task<ActionResult<RoleDto>> UpdateRole([FromBody] UpdateRoleDto dto)
    {
        var existing = await _roleService.GetByIdAsync(dto.Id);
        if (existing == null)
        {
            return NotFound("Role not found.");
        }

        var role = await _roleService.UpdateAsync(dto);
        if (role == null)
        {
            return NotFound("Role not found.");
        }

        await _auditLogger.LogAsync(
            EAuditAction.RoleUpdated,
            EAuditCategory.AccessControl,
            "Role",
            role.Id.ToString(),
            oldValues: JsonSerializer.Serialize(existing),
            newValues: JsonSerializer.Serialize(role));

        await _auditLogger.LogRoleAccessChangedAsync(
            role.Code,
            role.AccessFunctions.Select(accessFunction => accessFunction.Code),
            oldValues: JsonSerializer.Serialize(existing.AccessFunctions.Select(accessFunction => accessFunction.Code)),
            newValues: JsonSerializer.Serialize(role.AccessFunctions.Select(accessFunction => accessFunction.Code)));

        return Ok(role);
    }

    /// <summary>
    /// Deletes a non-system role.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlRolesManage)]
    public async Task<ActionResult> DeleteRole(int id)
    {
        var existing = await _roleService.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound("Role not found.");
        }

        await _roleService.DeleteAsync(id);

        await _auditLogger.LogAsync(
            EAuditAction.RoleDeleted,
            EAuditCategory.AccessControl,
            "Role",
            id.ToString(),
            oldValues: JsonSerializer.Serialize(existing));

        return NoContent();
    }

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlAssignmentsManage)]
    public async Task<ActionResult<UserRoleDto>> AssignRole([FromBody] AssignRoleDto dto)
    {
        var assignment = await _userRoleService.AssignRoleAsync(dto);
        await _auditLogger.LogRoleAssignedAsync(dto.UserId, assignment.RoleName, UserId);
        return Ok(assignment);
    }

    /// <summary>
    /// Removes a user-role assignment by assignment ID.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlAssignmentsManage)]
    public async Task<ActionResult> RemoveAssignment(int id)
    {
        var existing = (await _userRoleService.GetAllUserRolesAsync()).FirstOrDefault(assignment => assignment.Id == id);
        if (existing == null)
        {
            return NotFound("Assignment not found.");
        }

        var deleted = await _userRoleService.DeleteUserRoleAsync(id);
        if (!deleted)
        {
            return NotFound("Assignment not found.");
        }

        await _auditLogger.LogRoleRemovedAsync(existing.Username, existing.RoleName ?? existing.Role.ToString(), UserId);
        return NoContent();
    }
}
