using System.Text.Json.Serialization;

namespace Certstream.Models
{
    /// <summary>
    /// Primary object received in <see cref="ConnectionType.DomainsOnly"/> messages from Certstream.
    /// </summary>
    public struct DomainsOnlyMessage
    {
        [JsonPropertyName("data")]
        public string[] Hostnames { get; set; }

        [JsonPropertyName("message_type")]
        public string MessageType { get; set; }
    }
}