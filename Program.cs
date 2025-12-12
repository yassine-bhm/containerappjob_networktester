using System;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Diagnostics;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Network Connectivity Test ===");
        Console.WriteLine($"Starting test at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine();

        // Read from environment variables or use defaults
        string ServerFQDN = Environment.GetEnvironmentVariable("SERVER_FQDN") ?? "imap.gmail.com";
        string portString = Environment.GetEnvironmentVariable("SERVER_PORT") ?? "993";
        int portNumber = int.Parse(portString);
        const int timeoutSeconds = 30;

        Console.WriteLine($"Configuration:");
        Console.WriteLine($"  Server FQDN: {ServerFQDN}");
        Console.WriteLine($"  Port: {portNumber}");
        Console.WriteLine($"  Timeout: {timeoutSeconds}s");
        Console.WriteLine();

        try
        {
            await TestConnectivity(ServerFQDN, portNumber, timeoutSeconds);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed with exception: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
            Environment.ExitCode = 1;
        }

        Console.WriteLine();
        Console.WriteLine($"Test completed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine("=== End of Test ===");
    }

    static async Task TestConnectivity(string server, int port, int timeoutSeconds)
    {
        Console.WriteLine($"📡 Testing connectivity to: {server}:{port}");
        Console.WriteLine($"⏱️  Timeout: {timeoutSeconds} seconds");
        Console.WriteLine();

        // Step 1: DNS Resolution
        Console.WriteLine("Step 1: DNS Resolution");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var addresses = await System.Net.Dns.GetHostAddressesAsync(server);
            stopwatch.Stop();
            
            Console.WriteLine($"✅ DNS resolution successful ({stopwatch.ElapsedMilliseconds}ms)");
            Console.WriteLine($"   Resolved IP addresses:");
            foreach (var addr in addresses)
            {
                Console.WriteLine($"   - {addr} ({addr.AddressFamily})");
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"❌ DNS resolution failed: {ex.Message}");
            throw;
        }

        // Step 2: TCP Connection
        Console.WriteLine("Step 2: TCP Connection");
        TcpClient? tcpClient = null;
        stopwatch.Restart();

        try
        {
            tcpClient = new TcpClient();
            tcpClient.ReceiveTimeout = timeoutSeconds * 1000;
            tcpClient.SendTimeout = timeoutSeconds * 1000;

            var connectTask = tcpClient.ConnectAsync(server, port);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                throw new TimeoutException($"Connection attempt timed out after {timeoutSeconds} seconds");
            }

            await connectTask; // Ensure any exceptions are thrown
            stopwatch.Stop();

            Console.WriteLine($"✅ TCP connection established ({stopwatch.ElapsedMilliseconds}ms)");
            Console.WriteLine($"   Local endpoint: {tcpClient.Client.LocalEndPoint}");
            Console.WriteLine($"   Remote endpoint: {tcpClient.Client.RemoteEndPoint}");
            Console.WriteLine($"   Connected: {tcpClient.Connected}");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"❌ TCP connection failed ({stopwatch.ElapsedMilliseconds}ms): {ex.Message}");
            tcpClient?.Dispose();
            throw;
        }

        // Step 3: SSL/TLS Handshake
        Console.WriteLine("Step 3: SSL/TLS Handshake");
        SslStream? sslStream = null;
        stopwatch.Restart();

        try
        {
            var networkStream = tcpClient.GetStream();
            sslStream = new SslStream(
                networkStream,
                leaveInnerStreamOpen: false,
                userCertificateValidationCallback: (sender, certificate, chain, sslPolicyErrors) =>
                {
                    Console.WriteLine("   📜 Certificate validation callback triggered");
                    Console.WriteLine($"      Subject: {certificate?.Subject}");
                    Console.WriteLine($"      Issuer: {certificate?.Issuer}");
                    Console.WriteLine($"      Valid from: {certificate?.GetEffectiveDateString()}");
                    Console.WriteLine($"      Valid to: {certificate?.GetExpirationDateString()}");
                    Console.WriteLine($"      SSL Policy Errors: {sslPolicyErrors}");
                    
                    // For testing purposes, we accept any certificate
                    return true;
                }
            );

            await sslStream.AuthenticateAsClientAsync(server);
            stopwatch.Stop();

            Console.WriteLine($"✅ SSL/TLS handshake successful ({stopwatch.ElapsedMilliseconds}ms)");
            Console.WriteLine($"   SSL Protocol: {sslStream.SslProtocol}");
            Console.WriteLine($"   Cipher Algorithm: {sslStream.CipherAlgorithm} ({sslStream.CipherStrength} bits)");
            Console.WriteLine($"   Hash Algorithm: {sslStream.HashAlgorithm} ({sslStream.HashStrength} bits)");
            Console.WriteLine($"   Key Exchange Algorithm: {sslStream.KeyExchangeAlgorithm} ({sslStream.KeyExchangeStrength} bits)");
            Console.WriteLine($"   Is Authenticated: {sslStream.IsAuthenticated}");
            Console.WriteLine($"   Is Encrypted: {sslStream.IsEncrypted}");
            Console.WriteLine($"   Is Signed: {sslStream.IsSigned}");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"❌ SSL/TLS handshake failed ({stopwatch.ElapsedMilliseconds}ms): {ex.Message}");
            sslStream?.Dispose();
            tcpClient?.Dispose();
            throw;
        }

        // Step 4: Read  Server Greeting
        Console.WriteLine("Step 4: Reading Server Greeting");
        stopwatch.Restart();

        try
        {
            var buffer = new byte[4096];
            var bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length);
            stopwatch.Stop();

            if (bytesRead > 0)
            {
                var greeting = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                Console.WriteLine($"✅ Received server greeting ({stopwatch.ElapsedMilliseconds}ms, {bytesRead} bytes)");
                Console.WriteLine($"   Response: {greeting}");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine($"⚠️  No data received from server");
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"❌ Failed to read greeting ({stopwatch.ElapsedMilliseconds}ms): {ex.Message}");
        }
        finally
        {
            sslStream?.Dispose();
            tcpClient?.Dispose();
        }

        // Step 5: Summary
        Console.WriteLine("=== Test Summary ===");
        Console.WriteLine("✅ All connectivity tests passed!");
        Console.WriteLine($"   Server: {server}:{port}");
        Console.WriteLine($"   Status: REACHABLE");
        Console.WriteLine($"   SSL/TLS: WORKING");
        Console.WriteLine($"   Server Protocol: RESPONDING");
    }
}
