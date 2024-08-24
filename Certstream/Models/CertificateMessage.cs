using System.Text.Json.Serialization;

namespace Certstream.Models
{
    /// <summary>
    /// Primary object recived in <see cref="ConnectionType.Full"/> messages from Certstream.
    /// </summary>
    public struct CertificateMessage
    {
        [JsonPropertyName("data")]
        public MessageData Data { get; set; }

        [JsonPropertyName("message_type")]
        public string MessageType { get; set; }
    }
}