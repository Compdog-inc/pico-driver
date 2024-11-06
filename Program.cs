using DriverStation;
using DriverStation.Providers;

using var xbox = new CircuitPythonJoystickProvider();

HIDDeviceProvider.ErrorCode err;
switch (err = xbox.Connect())
{
    case HIDDeviceProvider.ErrorCode.NoDevices:
        Console.WriteLine("Could not find gamepad!");
        break;
    case HIDDeviceProvider.ErrorCode.ErrorConnecting:
        Console.WriteLine("Could not connect to gamepad!");
        break;
    case HIDDeviceProvider.ErrorCode.OK:
        Console.WriteLine("Gamepad connected!");
        break;
    default:
        Console.WriteLine("Unknown error: " + err);
        break;
}

using XboxClient xboxClient = new(xbox, "10.67.31.2", 5001, 5002);
xboxClient.Start();

using DSClient dsClient = new("10.67.31.2");
dsClient.Connect();

while (true)
{
}