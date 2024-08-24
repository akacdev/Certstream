using Certstream;
using Certstream.Models;
using Microsoft.Extensions.Logging;
using System;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddConsole();
});

var logger = loggerFactory.CreateLogger<CertstreamClient>();

var certstreamClient = new CertstreamClient(ConnectionType.Full, logger: logger);
await certstreamClient.StartAsync();

certstreamClient.CertificateIssued += (sender, cert) =>
{
    foreach (string domain in cert.AllDomains)
    {
        Console.WriteLine($"{cert.Issuer.O ?? cert.Issuer.CN} issued a SSL certificate for {domain}");
    }
};

Console.ReadKey();

await certstreamClient.StopAsync();

Console.ReadKey();
