namespace Application.Shared.Utilities;

//public static class CertificateHelpers
//{
//    public static (X509Certificate2 X509Certificate2, string PrivateKeyPem, string PublicKeyPem, string CertPem) CreateSelfSignedCertificate(string subjectNameCN)
//    {
//        using var rsa = RSA.Create(4096);

//        var req = new CertificateRequest($"CN={subjectNameCN}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

//        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10));

//        string privateKeyPem = PemEncoding.WriteString("PRIVATE KEY", cert.GetRSAPrivateKey()!.ExportPkcs8PrivateKey());
//        string publicKeyPem = PemEncoding.WriteString("PUBLIC KEY", cert.GetRSAPublicKey()!.ExportSubjectPublicKeyInfo());
//        string certPem = PemEncoding.WriteString("CERTIFICATE", cert.Export(X509ContentType.Cert));

//        return (cert, privateKeyPem, publicKeyPem, certPem);
//    }

//    public static (X509Certificate2 X509Certificate2, string PrivateKeyPem, string PublicKeyPem, string CertPem) CreateSelfSignedCertificate1(string subjectName)
//    {
//        using var rsa = RSA.Create(4096);

//        var req = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

//        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10));

//        string privateKeyPem = PemEncoding.WriteString("PRIVATE KEY", cert.GetRSAPrivateKey()!.ExportPkcs8PrivateKey());
//        string publicKeyPem = PemEncoding.WriteString("PUBLIC KEY", cert.GetRSAPublicKey()!.ExportSubjectPublicKeyInfo());
//        string certPem = PemEncoding.WriteString("CERTIFICATE", cert.Export(X509ContentType.Cert));

//        return (cert, privateKeyPem, publicKeyPem, certPem);
//    }

//    /// <summary>
//    /// Generates a self-signed Certificate Authority (CA) certificate
//    /// </summary>
//    /// <returns>X509Certificate2 with private key</returns>
//    public static X509Certificate2 GenerateCACertificate()
//    {
//        using var rsa = RSA.Create(2048);
//        var req = new CertificateRequest(
//            "CN=MQTT CA, O=Local MQTT, C=US",
//            rsa,
//            HashAlgorithmName.SHA256,
//            RSASignaturePadding.Pkcs1);

//        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
//        req.CertificateExtensions.Add(new X509KeyUsageExtension(
//            X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));

//        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
//        return new X509Certificate2(cert.Export(X509ContentType.Pfx), (string)null, X509KeyStorageFlags.Exportable);
//    }

//    /// <summary>
//    /// Generates a server certificate signed by the provided CA certificate
//    /// </summary>
//    /// <param name="caCert">The CA certificate used to sign the server certificate</param>
//    /// <returns>X509Certificate2 with private key</returns>
//    public static X509Certificate2 GenerateServerCertificate(X509Certificate2 caCert)
//    {
//        using var rsa = RSA.Create(2048);
//        var req = new CertificateRequest(
//            "CN=localhost, O=meldCX, C=AU",
//            rsa,
//            HashAlgorithmName.SHA256,
//            RSASignaturePadding.Pkcs1);

//        req.CertificateExtensions.Add(new X509KeyUsageExtension(
//            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));

//        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
//            [new Oid("1.3.6.1.5.5.7.3.1")], true)); // Server Authentication

//        // Add Subject Alternative Names
//        var sanBuilder = new SubjectAlternativeNameBuilder();
//        sanBuilder.AddDnsName("localhost");
//        sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
//        req.CertificateExtensions.Add(sanBuilder.Build());

//        var cert = req.Create(caCert, DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1), new byte[] { 1, 2, 3, 4 });
//        return new X509Certificate2(cert.CopyWithPrivateKey(rsa).Export(X509ContentType.Pfx), (string)null, X509KeyStorageFlags.Exportable);
//    }

//    /// <summary>
//    /// Saves a certificate and its private key to PEM files
//    /// </summary>
//    /// <param name="cert">The certificate to save</param>
//    /// <param name="certPath">Path where the certificate PEM file will be saved</param>
//    /// <param name="keyPath">Path where the private key PEM file will be saved</param>
//    public static async Task SaveCertificateAsync(X509Certificate2 cert, string certPath, string keyPath)
//    {
//        // Save certificate as PEM
//        var certPem = Convert.ToBase64String(cert.Export(X509ContentType.Cert));
//        var certContent = $"-----BEGIN CERTIFICATE-----\n{FormatPemBase64(certPem)}\n-----END CERTIFICATE-----\n";
//        await File.WriteAllTextAsync(certPath, certContent);

//        // Save private key as PEM
//        var rsa = cert.GetRSAPrivateKey();
//        var keyPem = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
//        var keyContent = $"-----BEGIN RSA PRIVATE KEY-----\n{FormatPemBase64(keyPem)}\n-----END RSA PRIVATE KEY-----\n";
//        await File.WriteAllTextAsync(keyPath, keyContent);
//    }

//    /// <summary>
//    /// Loads a certificate from PEM files (certificate + private key)
//    /// </summary>
//    /// <param name="certPath">Path to the certificate PEM file</param>
//    /// <param name="keyPath">Path to the private key PEM file</param>
//    /// <returns>X509Certificate2 with private key attached</returns>
//    public static X509Certificate2 LoadCertificateFromPem(string certPath, string keyPath)
//    {
//        var certPem = File.ReadAllText(certPath);
//        var keyPem = File.ReadAllText(keyPath);

//        // Extract base64 content from PEM
//        var certBase64 = ExtractBase64FromPem(certPem, "CERTIFICATE");
//        var keyBase64 = ExtractBase64FromPem(keyPem, "RSA PRIVATE KEY");

//        var certBytes = Convert.FromBase64String(certBase64);
//        var keyBytes = Convert.FromBase64String(keyBase64);

//        var cert = new X509Certificate2(certBytes);
//        var rsa = RSA.Create();
//        rsa.ImportRSAPrivateKey(keyBytes, out _);

//        return cert.CopyWithPrivateKey(rsa);
//    }

//    /// <summary>
//    /// Loads a certificate from a PEM file (certificate only, no private key)
//    /// </summary>
//    /// <param name="certPath">Path to the certificate PEM file</param>
//    /// <returns>X509Certificate2 without private key</returns>
//    public static X509Certificate2 LoadClientCertificateFromPem(string certPath)
//    {
//        var certPem = File.ReadAllText(certPath);
//        var certBase64 = ExtractBase64FromPem(certPem, "CERTIFICATE");
//        var certBytes = Convert.FromBase64String(certBase64);
//        return new X509Certificate2(certBytes);
//    }

//    /// <summary>
//    /// Formats base64 string into PEM format (64 characters per line)
//    /// </summary>
//    /// <param name="base64">Base64 encoded string</param>
//    /// <returns>Formatted base64 string with line breaks</returns>
//    private static string FormatPemBase64(string base64)
//    {
//        const int lineLength = 64;
//        var result = "";
//        for (int i = 0; i < base64.Length; i += lineLength)
//        {
//            var length = Math.Min(lineLength, base64.Length - i);
//            result += base64.Substring(i, length) + "\n";
//        }
//        return result.TrimEnd('\n');
//    }

//    /// <summary>
//    /// Extracts base64 content from a PEM formatted string
//    /// </summary>
//    /// <param name="pem">PEM formatted string</param>
//    /// <param name="type">The type of PEM content (e.g., "CERTIFICATE", "RSA PRIVATE KEY")</param>
//    /// <returns>Base64 encoded content without PEM headers/footers</returns>
//    private static string ExtractBase64FromPem(string pem, string type)
//    {
//        var startMarker = $"-----BEGIN {type}-----";
//        var endMarker = $"-----END {type}-----";

//        var startIndex = pem.IndexOf(startMarker) + startMarker.Length;
//        var endIndex = pem.IndexOf(endMarker);

//        if (startIndex < startMarker.Length || endIndex < 0)
//        {
//            throw new ArgumentException($"Invalid PEM format for {type}");
//        }

//        var base64 = pem[startIndex..endIndex];
//        return base64.Replace("\n", "").Replace("\r", "").Trim();
//    }
//}
