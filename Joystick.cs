namespace DriverStation
{
    public class Joystick
    {
        public float X = 0;
        public float Y = 0;

        private HidApi.Device? gamepad = null;

        public Joystick()
        {
        }

        public void InitializeHIDDevice()
        {
            var deviceDefinitions = HidApi.Hid.Enumerate(vendorId: 0x239A, productId: 0x80F4).Where(d => d.Usage == 5);

            if (deviceDefinitions.Count() == 0)
            {
                //No devices were found
                Console.WriteLine("No Gamepad devices found!");
                return;
            }

            Console.WriteLine("Found gamepad");
            //Get the device from its definition
            try
            {
                gamepad = deviceDefinitions.First().ConnectToDevice();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                gamepad = null;
            }

            if (gamepad != null)
            {
                Console.WriteLine("Gamepad initialized");
                _ = Task.Run(ReadHIDDevice);
            }
            else
            {
                Console.WriteLine("Failed to initialize gamepad");
            }
        }

        private void ReadHIDDevice()
        {
            while (gamepad != null)
            {
                var read = gamepad.Read(64);
                if (read.Length == 7)
                {
                    short x = BitConverter.ToInt16(read[3..]);
                    short y = BitConverter.ToInt16(read[5..]);
                    if (Math.Abs(x) > 300 || Math.Abs(y) > 300)
                    {
                        X = (float)x / short.MaxValue;
                        Y = (float)y / short.MaxValue;
                        Console.WriteLine("X: " + X + ", Y: " + Y);
                    }
                    else
                    {
                        X = 0;
                        Y = 0;
                    }
                }
                Task.Delay(1).Wait();
            }
        }
    }
}
