using MessagePack;
using NetCoreServer;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static DriverStation.DSClient;

namespace DriverStation
{
    public class WebServer : WsServer
    {
        class DriverStationAPISession : WsSession
        {
            public DriverStationAPISession(WsServer server) : base(server) { }

            public void SendDSClientStatus(SharedStatus status)
            {
                ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();
                MessagePackWriter writer = new MessagePackWriter(output);
                status.Serialize(ref writer);
                writer.Flush();
                SendBinaryAsync(output.WrittenSpan);
            }

            public override void OnWsConnected(HttpRequest request)
            {
                SendDSClientStatus(((WebServer)Server).lastStatus);
            }

            public override bool OnWsConnecting(HttpRequest request, HttpResponse response)
            {
                for (int i = 0; i < request.Headers; i++)
                {
                    (var key, var value) = request.Header(i);
                    if (key.Equals("Sec-WebSocket-Protocol", StringComparison.InvariantCultureIgnoreCase) && value.Equals("driverstation.webapp.msgpack", StringComparison.InvariantCultureIgnoreCase))
                    {
                        response.SetHeader("Sec-WebSocket-Protocol", value);
                        return true;
                    }
                }

                return false;
            }

            public override void OnWsReceivedText(byte[] buffer, long offset, long size)
            {
                string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
                Console.WriteLine("Incoming: " + message);
            }

            public override void OnWsReceivedBinary(byte[] buffer, long offset, long size)
            {
                MessagePackReader reader = new(new ReadOnlyMemory<byte>(buffer, (int)offset, (int)size));
                switch (reader.NextMessagePackType)
                {
                    case MessagePackType.Map:
                        {
                            int entries = reader.ReadMapHeader();
                            Dictionary<string, string> map = [];

                            for (int i = 0; i < entries; i++)
                            {
                                if (reader.NextMessagePackType == MessagePackType.String)
                                {
                                    string? key = reader.ReadString();
                                    if (key != null)
                                    {
                                        switch (reader.NextMessagePackType)
                                        {
                                            case MessagePackType.String:
                                                {
                                                    string? value = reader.ReadString();
                                                    map.Add(key, value ?? "null");
                                                    break;
                                                }
                                            case MessagePackType.Integer:
                                                {
                                                    int value = reader.ReadInt32();
                                                    map.Add(key, value.ToString());
                                                    break;
                                                }
                                        }
                                    }
                                }
                            }

                            ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();
                            MessagePackWriter writer = new MessagePackWriter(output);
                            writer.WriteMapHeader(entries);
                            foreach (var pair in map)
                            {
                                writer.Write(pair.Key);
                                writer.Write("echo_" + pair.Value);
                            }
                            writer.Flush();

                            SendBinaryAsync(output.WrittenSpan);

                            break;
                        }
                }
            }

            protected override void OnReceivedRequest(HttpRequest request)
            {
                base.OnReceivedRequest(request);

                if (WebSocket.WsHandshaked)
                    return;
                
                // Process HTTP request methods
                if (request.Method == "HEAD")
                    SendResponseAsync(Response.MakeHeadResponse());
                else if (request.Method == "GET")
                {
                    string path = request.Url;
                    if (path.StartsWith("/api/"))
                    {
                        SendResponseAsync(Response.MakeErrorResponse(404, "Endpoint does not exist."));
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(Path.GetFileName(path)))
                            path += "index";

                        if (!Path.HasExtension(path))
                            path += ".html"; // default html extension

                        var cached = Cache.Find(path);
                        if (cached.Item1)
                        {
                            SendAsync(cached.Item2);
                        }
                        else
                        {
                            SendAsync(Cache.Find("/404.html").Item2);
                        }
                    }
                }
                else if ((request.Method == "POST") || (request.Method == "PUT"))
                {
                    SendResponseAsync(Response.MakeOkResponse());   
                }
                else if (request.Method == "DELETE")
                {
                    SendResponseAsync(Response.MakeOkResponse());
                }
                else if (request.Method == "OPTIONS")
                    SendResponseAsync(Response.MakeOptionsResponse());
                else if (request.Method == "TRACE")
                    SendResponseAsync(Response.MakeTraceResponse(request.Cache.Data));
                else
                    SendResponseAsync(Response.MakeErrorResponse("Unsupported HTTP method: " + request.Method));
            }

            protected override void OnReceivedRequestError(HttpRequest request, string error)
            {
                base.OnReceivedRequestError(request, error);
                Console.WriteLine($"[WebServer]: request error: {error}");
            }

            protected override void OnError(SocketError error)
            {
                Console.WriteLine($"[WebServer]: session caught an error: {error}");
            }
        }

        private int port;
        private SharedStatus lastStatus;

        public WebServer(int port) : base(IPAddress.Any, port)
        {
            this.port = port;
            AddStaticContent("app", "");
        }

        protected override TcpSession CreateSession() { return new DriverStationAPISession(this); }

        private static IEnumerable<IPAddress>? _GetInterfaceAddresses_cached = null;
        private static Stopwatch? _GetInterfaceAddresses_cached_sw = null;
        const long _GetInterfaceAddresses_cached_refresh = 10000;
        // https://stackoverflow.com/a/60092903
        public IEnumerable<IPAddress>? GetInterfaceAddresses()
        {
            if (_GetInterfaceAddresses_cached == null || _GetInterfaceAddresses_cached_sw == null || _GetInterfaceAddresses_cached_sw.ElapsedMilliseconds >= _GetInterfaceAddresses_cached_refresh)
            {
                _GetInterfaceAddresses_cached = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(i => i.OperationalStatus == OperationalStatus.Up && i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(i => i.GetIPProperties().UnicastAddresses)
                    .Where(u => u.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(i => i.Address);
                _GetInterfaceAddresses_cached_sw ??= new();
                _GetInterfaceAddresses_cached_sw.Restart();
            }

            return _GetInterfaceAddresses_cached;
        }

        public void MulticastDSClientStatus(SharedStatus status)
        {
            lastStatus = status;
            ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();
            MessagePackWriter writer = new MessagePackWriter(output);
            status.Serialize(ref writer);
            writer.Flush();
            MulticastBinary(output.WrittenSpan);
        }

        protected override void OnStarted()
        {
            if (Endpoint is IPEndPoint endpoint)
            {
                if (endpoint.Address.Equals(IPAddress.Any))
                {
                    var addresses = GetInterfaceAddresses();
                    if (addresses == null)
                        Console.WriteLine($"[WebServer]: running on http://{endpoint.Address}:{endpoint.Port}");
                    else
                        Console.WriteLine($"[WebServer]: running on {string.Join(", ", addresses.Select(v => $"http://{v}:{endpoint.Port}"))}");
                }
                else
                {
                    Console.WriteLine($"[WebServer]: running on http://{endpoint.Address}:{endpoint.Port}");
                }
            }
            else
            {
                Console.WriteLine("[WebServer]: running with unsupported endpoint");
            }
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"[WebServer]: session caught an error: {error}");
        }
    }
}
