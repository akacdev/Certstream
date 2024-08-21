namespace Certstream.Models
{
    public enum ConnectionType
    {
        /// <summary>
        /// The default full connection type receives certificates with all information.
        /// </summary>
        Full,

        /// <summary>
        /// The domains-only mode only receives raw hostnames, resulting in less bandwidth used.
        /// </summary>
        DomainsOnly
    }
}