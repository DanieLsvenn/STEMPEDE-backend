using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Stemkit.Models;

public partial class User
{
    [Key]
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = null!;

    [MaxLength(255)]
    public string? Password { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = null!;

    public string? Phone { get; set; } 

    public string? Address { get; set; }

    public bool Status { get; set; }

    public bool IsExternal { get; set; }

    [MaxLength(50)]
    public string? ExternalProvider { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<SupportRequest> SupportRequests { get; set; } = new List<SupportRequest>();

    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
