using DriverStation;
using DriverStation.Providers;

using var webServer = new WebServer(5170);
webServer.Start();

using var xbox = new CircuitPythonJoystickProvider();
xbox.Updated += () =>
{
    webServer.MulticastJoystick(xbox.Packet);
};

HIDDeviceProvider.ErrorCode err;
switch (err = xbox.Connect())
{
    case HIDDeviceProvider.ErrorCode.NoDevices:
        Console.WriteLine("[HID]: Could not find gamepad!");
        break;
    case HIDDeviceProvider.ErrorCode.ErrorConnecting:
        Console.WriteLine("[HID]: Could not connect to gamepad!");
        break;
    case HIDDeviceProvider.ErrorCode.OK:
        Console.WriteLine("[HID]: Gamepad connected!");
        break;
    default:
        Console.WriteLine("[HID]: Unknown error: " + err);
        break;
}

using DSClient dsClient = new("10.67.31.2");
dsClient.StatusChanged += () =>
{
    webServer.MulticastDSClientStatus(dsClient.Status);
};

dsClient.Connect();

using XboxClient xboxClient = new(xbox, dsClient, "10.67.31.2", 5001, 5002);
xboxClient.Start();

while (true)
{
}