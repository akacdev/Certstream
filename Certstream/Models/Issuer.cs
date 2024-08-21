using System.Text.Json.Serialization;

namespace Certstream.Models
{
    public struct Issuer
    {
        /// <summary>
        /// X.509 Certficate attribute: CountryName
        /// </summary>
        [JsonPropertyName("C")]
        public string C { get; set; }

        /// <summary>
        /// X.509 Certficate attribute: CommonName
        /// </summary>
        [JsonPropertyName("CN")]
        public string CN { get; set; }

        /// <summary>
        /// X.509 Certficate attribute: Locality
        /// </summary>
        [JsonPropertyName("L")]
        public string L { get; set; }

        /// <summary>
        /// X.509 Certficate attribute: Organization
        /// </summary>
        [JsonPropertyName("O")]
        public string O { get; set; }

        /// <summary>
        /// X.509 Certficate attribute: OrganizationalUnit
        /// </summary>
        [JsonPropertyName("OU")]
        public string OU { get; set; }

        /// <summary>
        /// X.509 Certficate attribute: StateOrProvinceName
        /// </summary>
        [JsonPropertyName("ST")]
        public string ST { get; set; }

        [JsonPropertyName("aggregated")]
        public string Aggregated { get; set; }

        [JsonPropertyName("emailAddress")]
        public string EmailAddress { get; set; }
    }
}