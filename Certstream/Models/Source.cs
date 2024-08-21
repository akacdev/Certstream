using System.Text.Json.Serialization;

namespace Certstream.Models
{
    public struct Source
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}