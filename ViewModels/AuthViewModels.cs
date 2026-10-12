using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(100)]
    public string DisplayName { get; set; } = "";
    [Required, EmailAddress]
    public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Required, Compare(nameof(Password)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public class CreateUserViewModel : RegisterViewModel
{
    [Required]
    public string Role { get; set; } = "Sales Executive";
}

public class EditUserViewModel
{
    [Required] public string Id { get; set; } = "";
    [Required, StringLength(100)] public string DisplayName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Role { get; set; } = "Sales Executive";
    public bool IsActive { get; set; }
}

public class ResetPasswordViewModel
{
    [Required] public string UserId { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)] public string NewPassword { get; set; } = "";
    [Required, Compare(nameof(NewPassword)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
}
