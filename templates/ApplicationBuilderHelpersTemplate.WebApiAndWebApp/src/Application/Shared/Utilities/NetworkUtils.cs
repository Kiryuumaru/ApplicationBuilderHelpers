using System.Net;
using System.Net.Sockets;

namespace Application.Shared.Utilities;

public class NetworkUtils
{
    public static int GetRandomAvailablePort()
    {
        TcpListener l = new(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    public static int GetAvailablePort(int min = 20000, int max = 50000, string? seed = null)
    {
        Random random;
        if (string.IsNullOrEmpty(seed))
            random = new Random();
        else
            random = new Random(seed.GetHashCode());
        for (int i = 0; i < 1000; i++) // Try up to 1000 times
        {
            int port = random.Next(min, max + 1);
            if (IsPortAvailable(port))
                return port;
        }
        throw new InvalidOperationException($"No available port found in range {min}-{max}");
    }

    public static bool IsPortAvailable(int port)
    {
        try
        {
            using var tcpListener = new TcpListener(IPAddress.Loopback, port);
            tcpListener.Start();
            tcpListener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
