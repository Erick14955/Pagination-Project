using System.Text.Json.Serialization;

namespace Pagination_Project.Models
{
    public class SupabaseUserDto
    {
        [JsonPropertyName("Username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("Password")]
        public string Password { get; set; } = string.Empty;
    }
}