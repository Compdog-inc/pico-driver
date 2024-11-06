using DriverStation.Providers;
using System.Net.Sockets;

namespace DriverStation
{
    public class XboxClient(IXboxProvider xbox, string hostname, int remotePort, int localPort) : IDisposable
    {
        private readonly UdpClient client = new(localPort);

        public string Hostname { get => hostname; set => hostname = value; }
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
            client.Close();
            clientTask?.Wait(100);
            clientTask?.Dispose();
            clientTask = null;
        }

        private void ClientLoop()
        {
            try
            {
                client.DontFragment = true;
            }
            catch
            {
                Console.WriteLine("[XBOX]: Could not set fragment preference.");
            }

            while (started)
            {
                try
                {
                    Console.WriteLine("[XBOX]: Client connecting...");
                    client.Connect(hostname, RemotePort);
                    Console.WriteLine("[XBOX]: Connection established.");
                }
                catch
                {
                    Console.WriteLine("[XBOX]: Error connecting.");
                    continue;
                }

                byte sessionId = (byte)Random.Shared.Next(255 /* 0-254 inclusive, 255 is reserved for host session */);
                uint packetTimestamp = 0;

                while (started)
                {
                    try
                    {
                        client.Send(xbox.Packet.toBytes().SerializeWith(new(new Header(sessionId, packetTimestamp++))));
                        Task.Delay(20).Wait();
                    }
                    catch
                    {
                        Console.WriteLine("[XBOX]: Connection crashed!");
                        client.Dispose();
                        break;
                    }
                }

                Task.Delay(20).Wait();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Stop();
            client.Dispose();
        }
    }
}
