using Certstream;
using Certstream.Models;
using System;


var certstreamClient = new CertstreamClient(ConnectionType.Full);
await certstreamClient.StartAsync();

certstreamClient.CertificateIssued += (sender, cert) =>
{
    foreach (string domain in cert.AllDomains)
    {
        Console.WriteLine($"{cert.Issuer.O ?? cert.Issuer.CN} issued a SSL certificate for {domain}");
    }
};

Console.ReadKey();
