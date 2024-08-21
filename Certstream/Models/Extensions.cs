using System.Text.Json.Serialization;

namespace Certstream.Models
{
    public struct Extensions
    {
        [JsonPropertyName("authorityInfoAccess")]
        public string AuthorityInfoAccess { get; set; }

        [JsonPropertyName("authorityKeyIdentifier")]
        public string AuthorityKeyIdentifier { get; set; }

        [JsonPropertyName("basicConstraints")]
        public string BasicConstraints { get; set; }

        [JsonPropertyName("certificatePolicies")]
        public string CertificatePolicies { get; set; }

        [JsonPropertyName("ctlSignedCertificateTimestamp")]
        public string CtlSignedCertificateTimestamp { get; set; }

        [JsonPropertyName("extendedKeyUsage")]
        public string ExtendedKeyUsage { get; set; }

        [JsonPropertyName("keyUsage")]
        public string KeyUsage { get; set; }

        [JsonPropertyName("subjectAltName")]
        public string SubjectAltName { get; set; }

        [JsonPropertyName("subjectKeyIdentifier")]
        public string SubjectKeyIdentifier { get; set; }

        [JsonPropertyName("ctlPoisonByte")]
        public bool? CtlPoisonByte { get; set; }

        [JsonPropertyName("crlDistributionPoints")]
        public string CrlDistributionPoints { get; set; }
    }
}