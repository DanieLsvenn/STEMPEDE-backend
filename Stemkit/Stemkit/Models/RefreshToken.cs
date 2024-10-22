﻿using System;
using System.Collections.Generic;

namespace Stemkit.Models;

public partial class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime ExpirationTime { get; set; }

    public DateTime? Revoked { get; set; }

    public string? RevokedByIp { get; set; }

    public string? ReplacedByToken { get; set; }

    public string Token { get; set; } = null!;

    public DateTime Created { get; set; }

    public string CreatedByIp { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
