using Certstream;
using Certstream.Models;
using System;

namespace Example
{
    public static class Program
    {
        public static readonly CertstreamClient Client = new(ConnectionType.Full);

        public static void Main()
        {
            Client.CertificateIssued += (sender, cert) =>
            {
                foreach (string domain in cert.AllDomains)
                {
                    Console.WriteLine($"{cert.Issuer.O ?? cert.Issuer.CN} issued a SSL certificate for {domain}");
                }
            };

            Console.ReadKey();
        }
    }
}