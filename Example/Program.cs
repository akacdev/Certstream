using Certstream;
using System;

namespace Example
{
    public static class Program
    {
        public static void Main()
        {
            CertstreamClient certstream = new(ConnectionType.Full);

            certstream.CertificateIssued += (sender, cert) =>
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