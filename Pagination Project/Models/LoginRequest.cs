using System.ComponentModel.DataAnnotations;

namespace Pagination_Project.Models
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "User required.")]
        [MinLength(4, ErrorMessage = "minimum 4 characters.")]
        [MaxLength(20, ErrorMessage = "Máximum 20 characters.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ]+$", ErrorMessage = "Letters only, no numbers or special characters.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password required.")]
        [MinLength(6, ErrorMessage = "Mínimum 6 characters.")]
        [MaxLength(30, ErrorMessage = "Máximum 30 characters.")]
        [RegularExpression(@"^\S+$", ErrorMessage = "Password cannot contain spaces.")]
        public string Password { get; set; } = string.Empty;
    }
}