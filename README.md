# Server Connectivity Test - Container App Job

This project is a .NET 8 console application that tests network connectivity to any server. It's designed to run as an Azure Container Apps Job for manual execution.

## Features

- ✅ DNS resolution test
- ✅ TCP connection test
- ✅ SSL/TLS handshake verification
- ✅ Server greeting validation
- ✅ Detailed logging with timestamps
- ✅ Network-level connectivity testing (no authentication required)

## Local Testing

### Build and run locally

```bash
dotnet build
dotnet run
```

## Expected Output:

=== Server Connectivity Test to imap.gmail.com ===
Starting test at: 2025-12-12 10:30:00 UTC

📡 Testing connectivity to: imap.gmail.com:993
⏱️  Timeout: 30 seconds

Step 1: DNS Resolution
✅ DNS resolution successful (XXms)
   Resolved IP addresses:
   - 142.250.xxx.xxx (InterNetwork)
   ...

Step 2: TCP Connection
✅ TCP connection established (XXms)
   Local endpoint: xxx.xxx.xxx.xxx:xxxxx
   Remote endpoint: xxx.xxx.xxx.xxx:993
   Connected: True

Step 3: SSL/TLS Handshake
✅ SSL/TLS handshake successful (XXms)
   SSL Protocol: Tls13
   Cipher Algorithm: Aes256 (256 bits)
   ...

Step 4: Reading  Server Greeting
✅ Received Server  greeting (XXms, XX bytes)
   Response: * OK Server ready...

=== Test Summary ===
✅ All connectivity tests passed!
