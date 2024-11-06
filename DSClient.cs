using NetCoreServer;
using System.Net.Sockets;
using System.Text;
using WatchdogDotNet;
using Timer = System.Timers.Timer;

namespace DriverStation
{
    public class DSClient : IDisposable
    {
        private static readonly TimeSpan timeout = TimeSpan.FromSeconds(2);

        private class Client(string url) : WsClient(ParseUrl(url).address, ParseUrl(url).port)
        {
            private Timer? watchdog;

            public Guid latestPingId;

            private static (string host, string path, string address, int port) ParseUrl(string url)
            {
                int host_start = url.IndexOf('/') + 2;
                int host_end = url.IndexOf('/', host_start);
                string host = url[host_start..host_end];
                string path = url[host_end..];

                int hostSep = host.IndexOf(':');
                string serverStr;
                int port;
                if (hostSep != -1)
                {
                    serverStr = host[..hostSep];
                    string portStr = host[(hostSep + 1)..];
                    port = int.Parse(portStr);
                }
                else
                {
                    serverStr = host;
                    port = 80;
                }

                return (host, path, serverStr, port);
            }

            public void DisconnectAndStop()
            {
                _stop = true;
                CloseAsync(1000);
                while (IsConnected)
                    Thread.Yield();
            }

            public bool ConnectAndDontStop()
            {
                _stop = false;
                Console.WriteLine($"[DSClient]: Client connecting...");
                return ConnectAsync();
            }

            public override void OnWsConnecting(HttpRequest request)
            {
                var parts = ParseUrl(url);
                request.SetBegin("GET", parts.path);
                request.SetHeader("Host", parts.host);
                request.SetHeader("Upgrade", "websocket");
                request.SetHeader("Connection", "Upgrade");
                request.SetHeader("Sec-WebSocket-Key", Convert.ToBase64String(WsNonce));
                request.SetHeader("Sec-WebSocket-Protocol", "driverstation.pico.rover");
                request.SetHeader("Sec-WebSocket-Version", "13");
                request.SetBody();
            }

            public override void OnWsConnected(HttpResponse response)
            {
                Console.WriteLine($"[DSClient]: Connection established.");
                watchdog?.Dispose();
                watchdog = new(timeout)
                {
                    AutoReset = false
                };
                watchdog.Elapsed += (s, e) => { Disconnect(); };
            }

            bool wsDisconnected = false;
            public override void OnWsDisconnected()
            {
                wsDisconnected = true;
                Console.WriteLine($"[DSClient]: Connection lost.");
                watchdog?.Dispose();
            }

            public override void OnWsReceived(byte[] buffer, long offset, long size)
            {
                Console.WriteLine($"Incoming: {Encoding.UTF8.GetString(buffer, (int)offset, (int)size)}");
            }

            public override void OnWsPing(byte[] buffer, long offset, long size)
            {
                base.OnWsPing(buffer, offset, size);
            }

            public override void OnWsPong(byte[] buffer, long offset, long size)
            {
                base.OnWsPong(buffer, offset, size);
                if (latestPingId.Equals(new Guid(new ReadOnlySpan<byte>(buffer, (int)offset, (int)size))))
                    watchdog?.Restart();
            }

            protected override void OnDisconnected()
            {
                base.OnDisconnected();

                if(!wsDisconnected)
                    Console.WriteLine($"[DSClient]: Connection lost.");
                wsDisconnected = false;

                Thread.Sleep(100);

                if (!_stop)
                {
                    Console.WriteLine($"[DSClient]: Client connecting...");
                    ConnectAsync();
                }
            }

            protected override void OnError(SocketError error)
            {
                Console.WriteLine($"[DSClient]: error {error}");
            }

            private bool _stop;
        }

        private Client client;

        public bool IsConnected => client.IsConnected;

        public DSClient(string address)
        {
            client = new Client("ws://" + address + ":5002/");
        }

        public bool Connect()
        {
            bool res = client.ConnectAndDontStop();
            _ = Task.Run(() =>
            {
                while (true)
                {
                    client.latestPingId = Guid.NewGuid();
                    client.SendPingAsync(client.latestPingId.ToByteArray());
                    Task.Delay(1000).Wait();
                }
            });
            return res;
        }

        public void Disconnect()
        {
            client.DisconnectAndStop();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Disconnect();
            client.Dispose();
        }
    }
}
