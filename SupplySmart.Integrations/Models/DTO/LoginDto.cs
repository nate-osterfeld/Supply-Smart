using System.ComponentModel.DataAnnotations;

namespace SupplySmart.Integrations.Models.DTO;

public class LoginDto
{
    [Required, EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
    [Required]
    [StringLength(1024)]
    public string Password { get; set; } = string.Empty;
}
