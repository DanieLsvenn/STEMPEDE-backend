using System;
using System.Collections.Generic;

namespace Stemkit.Models;

public partial class UserPermission
{
    public int UserPermissionId { get; set; }

    public int UserId { get; set; }

    public int PermissionId { get; set; }

    public int AssignedBy { get; set; }

    public virtual User AssignedByNavigation { get; set; } = null!;

    public virtual Permission Permission { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
