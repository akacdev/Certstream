using Certstream;
using Certstream.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Example
{
    public class Example
    {
        public static async Task Main()
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddConsole();
            });

            ILogger<CertstreamClient> logger = loggerFactory.CreateLogger<CertstreamClient>();

            CertstreamClient client = new(ConnectionType.Full, logger: logger);
            await client.StartAsync();

            client.CertificateIssued += (sender, cert) =>
            {
                foreach (string domain in cert.AllDomains)
                    Console.WriteLine($"{cert.Issuer.O ?? cert.Issuer.CN} issued a SSL certificate for {domain}");
            };

            Console.ReadKey(true);

            await client.StopAsync();

            Console.ReadKey(true);
        }
    }
}