using System;
using System.IO;
using System.Threading.Tasks;
using Renci.SshNet;

namespace hum.Tools
{
    /// <summary>
    /// Debug tool to test SSH key compatibility with SSH.NET
    /// </summary>
    /// <remarks>
    /// To use this tool:
    /// 1. Add it to your project
    /// 2. Call it from your code when testing keys
    /// </remarks>
    public class SshKeyTester
    {
        /// <summary>
        /// Test a private key for compatibility with SSH.NET
        /// </summary>
        public static void TestKey(string keyPath, string? passphrase = null)
        {
            Console.WriteLine($"Testing SSH key: {keyPath}");
            
            try
            {
                // First check if file exists
                if (!File.Exists(keyPath))
                {
                    Console.WriteLine("  ❌ File not found!");
                    return;
                }

                // Try to read the key
                Console.WriteLine("  📄 Reading key file...");
                string content = File.ReadAllText(keyPath);
                
                Console.WriteLine($"  Key length: {content.Length} characters");
                Console.WriteLine($"  First 30 chars: {content.Substring(0, Math.Min(30, content.Length)).Replace("\n", "\\n").Replace("\r", "\\r")}");
                
                // Try to load the key
                Console.WriteLine("  🔑 Loading key with SSH.NET...");
                PrivateKeyFile keyFile;
                
                if (string.IsNullOrEmpty(passphrase))
                {
                    keyFile = new PrivateKeyFile(keyPath);
                }
                else
                {
                    keyFile = new PrivateKeyFile(keyPath, passphrase);
                }
                
                Console.WriteLine("  ✅ Key loaded successfully!");
                
                // SSH.NET doesn't expose HostKey directly, so we'll just verify the key was loaded
                Console.WriteLine("  Key loaded and validated by SSH.NET");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Error: {ex.GetType().Name} - {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"  ❌ Inner exception: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// Comprehensive SSH connection test
        /// </summary>
        public static async Task TestConnection(string host, string username, string keyPath, string passphrase = null)
        {
            Console.WriteLine($"Testing SSH connection to {username}@{host} using key {keyPath}");
            
            try
            {
                // First test the key
                TestKey(keyPath, passphrase);
                
                // Now try connection
                Console.WriteLine("  🌐 Connecting to server...");
                
                PrivateKeyFile keyFile = passphrase == null 
                    ? new PrivateKeyFile(keyPath) 
                    : new PrivateKeyFile(keyPath, passphrase);
                
                var connectionInfo = new ConnectionInfo(
                    host, 
                    username,
                    new PrivateKeyAuthenticationMethod(username, keyFile)
                );
                
                using (var client = new SshClient(connectionInfo))
                {
                    await Task.Run(() => client.Connect());
                    
                    if (client.IsConnected)
                    {
                        Console.WriteLine("  ✅ Connected successfully!");
                        
                        // Try a simple command
                        var result = client.RunCommand("echo Connection successful");
                        Console.WriteLine($"  🖥️  Command output: {result.Result}");
                        
                        client.Disconnect();
                        Console.WriteLine("  👋 Disconnected");
                    }
                    else
                    {
                        Console.WriteLine("  ❌ Failed to connect");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Error: {ex.GetType().Name} - {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"  ❌ Inner exception: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                }
            }
        }
    }
}
