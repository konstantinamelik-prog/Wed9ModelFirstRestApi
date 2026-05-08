using System.ComponentModel.DataAnnotations;

namespace SchoolApp.DTO
{
    public record UserLoginDTO
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Username must be between 2 and 50 characters.")]
        public string Username { get; init; } = string.Empty;

        [Required(ErrorMessage = "The {0} field is required.")]
        [RegularExpression(@"(?=.*?[A-Z])(?=.*?[a-z])(?=.*?\d)(?=.*?\W)^.{8,}$",
            ErrorMessage = "Password must contain at least 8 characters, including at least one uppercase, one lowercase, " +
            "one digit, and one special character")]
        public string Password { get; init; } = string.Empty;

        public bool KeepLoggedIn { get; init; }
    }
}
