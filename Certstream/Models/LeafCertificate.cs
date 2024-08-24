using System;
using System.Text.Json.Serialization;

namespace Certstream.Models
{
    public struct LeafCertificate
    {
        [JsonPropertyName("all_domains")]
        public string[] AllDomains { get; set; }

        [JsonPropertyName("extensions")]
        public Extensions Extensions { get; set; }

        [JsonPropertyName("fingerprint")]
        public string Fingerprint { get; set; }

        [JsonPropertyName("issuer")]
        public Issuer Issuer { get; set; }

        [JsonConverter(typeof(UnixDateTimeOffsetConverter))]
        [JsonPropertyName("not_after")]
        public DateTimeOffset NotAfter { get; set; }

        [JsonConverter(typeof(UnixDateTimeOffsetConverter))]
        [JsonPropertyName("not_before")]
        public DateTimeOffset NotBefore { get; set; }

        [JsonPropertyName("serial_number")]
        public string SerialNumber { get; set; }

        [JsonPropertyName("signature_algorithm")]
        public string SignatureAlgorithm { get; set; }

        [JsonPropertyName("subject")]
        public Subject Subject { get; set; }
    }
}