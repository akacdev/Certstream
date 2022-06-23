using System.Text.Json.Serialization;

namespace Certstream
{

    public struct CertMessage
    {
        [JsonPropertyName("data")]
        public MessageData Data { get; set; }

        [JsonPropertyName("message_type")]
        public string MessageType { get; set; }
    }

    public struct MessageData
    {
        [JsonPropertyName("cert_index")]
        public int CertIndex { get; set; }

        [JsonPropertyName("cert_link")]
        public string CertLink { get; set; }

        [JsonPropertyName("leaf_cert")]
        public LeafCert LeafCert { get; set; }

        [JsonPropertyName("seen")]
        public double Seen { get; set; }

        [JsonPropertyName("source")]
        public Source Source { get; set; }

        [JsonPropertyName("update_type")]
        public string UpdateType { get; set; }
    }

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

    public struct Issuer
    {
        [JsonPropertyName("C")]
        public string C { get; set; }

        [JsonPropertyName("CN")]
        public string CN { get; set; }

        [JsonPropertyName("L")]
        public string L { get; set; }

        [JsonPropertyName("O")]
        public string O { get; set; }

        [JsonPropertyName("OU")]
        public object OU { get; set; }

        [JsonPropertyName("ST")]
        public string ST { get; set; }

        [JsonPropertyName("aggregated")]
        public string Aggregated { get; set; }

        [JsonPropertyName("emailAddress")]
        public object EmailAddress { get; set; }
    }

    public struct LeafCert
    {
        [JsonPropertyName("all_domains")]
        public string[] AllDomains { get; set; }

        [JsonPropertyName("extensions")]
        public Extensions Extensions { get; set; }

        [JsonPropertyName("fingerprint")]
        public string Fingerprint { get; set; }

        [JsonPropertyName("issuer")]
        public Issuer Issuer { get; set; }

        [JsonPropertyName("not_after")]
        public int NotAfter { get; set; }

        [JsonPropertyName("not_before")]
        public int NotBefore { get; set; }

        [JsonPropertyName("serial_number")]
        public string SerialNumber { get; set; }

        [JsonPropertyName("signature_algorithm")]
        public string SignatureAlgorithm { get; set; }

        [JsonPropertyName("subject")]
        public Subject Subject { get; set; }
    }

    public struct Source
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public struct Subject
    {
        [JsonPropertyName("C")]
        public object C { get; set; }

        [JsonPropertyName("CN")]
        public string CN { get; set; }

        [JsonPropertyName("L")]
        public object L { get; set; }

        [JsonPropertyName("O")]
        public object O { get; set; }

        [JsonPropertyName("OU")]
        public object OU { get; set; }

        [JsonPropertyName("ST")]
        public object ST { get; set; }

        [JsonPropertyName("aggregated")]
        public string Aggregated { get; set; }

        [JsonPropertyName("emailAddress")]
        public object EmailAddress { get; set; }
    }
}
