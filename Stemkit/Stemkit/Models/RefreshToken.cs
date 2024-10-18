using System;
using System.Collections.Generic;

namespace Stemkit.Models;

public partial class RefreshToken
{
    public int Id { get; set; }

    public string Token { get; set; } = null!;

    public int? UserId { get; set; }

    public DateTime Expires { get; set; }

    public DateTime Created { get; set; }

    public string? CreatedByIp { get; set; }

    public virtual User? User { get; set; }
}
