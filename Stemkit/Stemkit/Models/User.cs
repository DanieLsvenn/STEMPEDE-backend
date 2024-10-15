using System;
using System.Collections.Generic;

namespace Stemkit.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Staff? Staff { get; set; }

    public virtual ICollection<UserPermission> UserPermissionAssignedByNavigations { get; set; } = new List<UserPermission>();

    public virtual ICollection<UserPermission> UserPermissionUsers { get; set; } = new List<UserPermission>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
