using System.Text.Json.Serialization;

namespace Pagination_Project.Models
{
    public class SupabaseUserDto
    {
        [JsonPropertyName("ID")]
        public Guid ID { get; set; }  

        [JsonPropertyName("Username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("Password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;
    }
}