using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SupplySmart.Integrations.Models.DTO;

public class RegisterDto
{
    [Required]
    [StringLength(200)]
    public string OrganizationName { get; set; } = string.Empty;
    [Required]
    [StringLength(150)]
    public string AdminFullName { get; set; } = string.Empty;
    [Required, EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
    [Required]
    [StringLength(1024, MinimumLength = 12, ErrorMessage = "Use a password between 12 and 1024 characters.")]
    public string Password { get; set; } = string.Empty;
}
