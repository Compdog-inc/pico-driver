using DriverStation;
using System.Net.Sockets;

UdpClient? client;

Joystick joystick = new Joystick();
joystick.InitializeHIDDevice();

while (true)
{
    client = new UdpClient(5002);
    client.DontFragment = true;

    Console.WriteLine("Client connecting...");
    client.Connect("10.67.31.2", 5001);

    byte sessionId = (byte)Random.Shared.Next(255 /* 0-254 inclusive, 255 is reserved for host session */);
    uint packetTimestamp = 0;

    while (true)
    {
        try
        {
            XboxPacket packet = new XboxPacket();
            packet.axis_X = XboxPacket.setAxis(-joystick.X);
            packet.axis_Y = XboxPacket.setAxis(-joystick.Y);

            client.Send(packet.toBytes().injectHeader(new SerializedPacket.Header(sessionId, packetTimestamp++)));

            Task.Delay(20).Wait();
        } catch
        {
            Console.WriteLine("Connection crashed!");
            client.Dispose();
            break;
        }
    }
}