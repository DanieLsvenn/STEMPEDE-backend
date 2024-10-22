using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Stemkit.Models;

public partial class RefreshToken
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime ExpirationTime { get; set; }

    public DateTime? Revoked { get; set; }

    [MaxLength(45)]
    public string? RevokedByIp { get; set; }

    [MaxLength(255)]
    public string? ReplacedByToken { get; set; }

    public string Token { get; set; } = null!;

    public DateTime Created { get; set; }

    [Required]
    [MaxLength(45)]

    public string CreatedByIp { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    // Computed property to determine if the token is revoked
    [NotMapped]
    public bool IsRevoked => Revoked.HasValue;
}
