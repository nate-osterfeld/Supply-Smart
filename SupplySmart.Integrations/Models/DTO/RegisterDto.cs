using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SupplySmart.Integrations.Models.DTO;

public class RegisterDto
{
    [Required]
    public string OrganizationName { get; set; } = string.Empty;
    [Required]
    public string AdminFullName { get; set; } = string.Empty;
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
}
