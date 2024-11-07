using DriverStation.Providers;
using System.Net.Sockets;

namespace DriverStation
{
    public class XboxClient(IXboxProvider xbox, DSClient driverstation, string hostname, int remotePort, int localPort) : IDisposable
    {
        private UdpClient? client;

        public string Hostname { get => hostname; set => hostname = value; }
        public int LocalPort { get => localPort; set => localPort = value; }
        public int RemotePort { get => remotePort; set => remotePort = value; }
        public IXboxProvider Xbox { get => xbox; set => xbox = value; }

        private bool started = false;
        private Task? clientTask;

        public bool Started => started;

        public void Start()
        {
            started = true;
            clientTask = new Task(ClientLoop);
            clientTask.Start();
        }

        public void Stop()
        {
            started = false;
            client?.Close();
            clientTask?.Wait(100);
            clientTask?.Dispose();
            clientTask = null;
        }

        private void ClientLoop()
        {
            while (started)
            {
                Task.Delay(20).Wait();
                try
                {
                    client = new UdpClient(LocalPort);
                } catch
                {
                    Console.WriteLine("[XBOX]: Error binding socket.");
                    continue;
                }

                try
                {
                    client.DontFragment = true;
                }
                catch
                {
                    Console.WriteLine("[XBOX]: Could not set fragment preference.");
                }

                try
                {
                    Console.WriteLine("[XBOX]: Client connecting...");
                    client.Connect(hostname, RemotePort);
                    Console.WriteLine("[XBOX]: Connection established.");
                }
                catch
                {
                    Console.WriteLine("[XBOX]: Error connecting.");
                    client.Dispose();
                    continue;
                }

                byte sessionId = (byte)Random.Shared.Next(255 /* 0-254 inclusive, 255 is reserved for host session */);

                while (started)
                {
                    try
                    {
                        client.Send(xbox.Packet.toBytes().SerializeWith(new(new Header(sessionId, (ulong)driverstation.ServerTimeUs))));
                        Task.Delay(20).Wait();
                    }
                    catch
                    {
                        Console.WriteLine("[XBOX]: Connection crashed!");
                        client.Dispose();
                        break;
                    }
                }
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Stop();
            client?.Dispose();
        }
    }
}
