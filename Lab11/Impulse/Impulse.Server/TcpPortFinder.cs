using System.Net.Sockets;

namespace Impulse.Server
{
    internal static class TcpPortFinder
    {
        public static bool TryGetAvailablePort(int start, int end, out int port)
        {
            var length = end - start;
            for (var i = 0; i <= length; ++i)
            {
                port = start + i;
                if (TryPort(port))
                {
                    return true;
                }
            }

            port = -1;
            return false;
        }

        private static bool TryPort(int port)
        {
            var listener = TcpListener.Create(port);
            try
            {
                listener.ExclusiveAddressUse = true;
                listener.Start();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}