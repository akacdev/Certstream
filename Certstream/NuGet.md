# Certstream

![](https://raw.githubusercontent.com/actually-akac/Certstream/master/Certstream/icon.png)

A C# library for processing newly issued SSL certificates in real time using the Certstream API.

## Usage
This library provides an easy interface for interacting with the Certstream API, allowing you to process newly issued SSL/TLS certificates in real time.

You can either listen for all spotted certificates with all details available or save on bandwidth by only requesting raw hostnames, if applicable.
By default, the library will connect to a public Certstream server instance hosted by Calidog, but you can specify a custom instance instead.

To get started, import the library into your solution with either the `NuGet Package Manager` or the `dotnet` CLI.
```rust
dotnet add package Certstream
```

For the primary classes to become available, import the used namespace.
```csharp
using Certstream;
```

Need more examples? Under the `Example` directory you can find a working demo project that implements this library.

## Properties
- Built for **.NET 8**, **.NET 7** and **.NET 6**
- Fully **async**
- Extensive **XML documentation**

## Features
- Connect to a Certstream server and receive certificates in real time
- Parse and process over 200 issued certificates per second
- Automatically reconnect when connection is lost
- Updates are exposed through events

## References
- https://certstream.calidog.io/
- https://github.com/CaliDog/certstream-server
- https://certificate.transparency.dev/

```
Google Trust Services LLC issued a SSL certificate for bestbuiltcon.biz
cPanel, Inc. issued a SSL certificate for shreeramns.com
DigiCert, Inc. issued a SSL certificate for myaccounttool.sunwebgroup.com
Let's Encrypt issued a SSL certificate for *.degarage.ch
Cloudflare, Inc. issued a SSL certificate for *.charlottetownbutcherpei.ca
DigiCert, Inc. issued a SSL certificate for margarete-blarer.de
Amazon issued a SSL certificate for *.us-west-2.es.amazonaws.com
DigiCert, Inc. issued a SSL certificate for remote-helpdesk.addaxmotors.com
Let's Encrypt issued a SSL certificate for *.diogoantonio.autocode.gg
Cloudflare, Inc. issued a SSL certificate for *.popracanaho.tk
Cloudflare, Inc. issued a SSL certificate for *.weirogiljare.tk
Amazon issued a SSL certificate for *.ap-northeast-3.es.amazonaws.com
cPanel, Inc. issued a SSL certificate for sawanschoolsirsa.developerszone.in
Amazon issued a SSL certificate for *.us-east-2.es.amazonaws.com
cPanel, Inc. issued a SSL certificate for webdisk.davidictrade.org.za
Cloudflare, Inc. issued a SSL certificate for *.africantelevisiononline.com
Let's Encrypt issued a SSL certificate for *.e-davetiyem.com
Cloudflare, Inc. issued a SSL certificate for *.independentguide.net
cPanel, Inc. issued a SSL certificate for webdisk.indiyaa.in
Amazon issued a SSL certificate for 69f3d881-9deb-4a2b-a0f7-70558c2b43a1.forgeapps.ec2.aws.dev
Cloudflare, Inc. issued a SSL certificate for *.autocarose.tech
Let's Encrypt issued a SSL certificate for *.divichangelog.com
Let's Encrypt issued a SSL certificate for *.dream.opstella.in.th
Let's Encrypt issued a SSL certificate for *.dub200.autocode.gg
Cloudflare, Inc. issued a SSL certificate for *.yldsn.top
cPanel, Inc. issued a SSL certificate for runwal-thane.in
DigiCert Inc issued a SSL certificate for hanacloud.ondemand.com
Cloudflare, Inc. issued a SSL certificate for *.circhaufluticcar.tk
Actalis S.p.A. issued a SSL certificate for sanguedolcecostruzioni.it
Cloudflare, Inc. issued a SSL certificate for *.service-coio.coi-occitanie.workers.dev
Google Trust Services LLC issued a SSL certificate for coisearlamitkea.tk
DigiCert Inc issued a SSL certificate for burstableflexrunnerserver637915813880232072.postgres.database.azure.com
Amazon issued a SSL certificate for *.ds-2016.env.polar.com
Let's Encrypt issued a SSL certificate for *.daniels-fotoexzesse.de
```