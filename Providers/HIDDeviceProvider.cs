using System.Diagnostics;

namespace DriverStation.Providers
{
    public abstract class HIDDeviceProvider : IXboxProvider, IDisposable
    {
        public enum ErrorCode : uint
        {
            OK = 0,
            AlreadyConnected = 1,
            NoDevices = 2,
            ErrorConnecting = 3,
            NotConnected = 4
        }

        public abstract XboxPacket Packet { get; }

        protected abstract ushort VendorId { get; }
        protected abstract ushort ProductId { get; }
        protected abstract ushort Usage { get; }

        protected virtual int MaxPacketLength { get => 64; }

        private HidApi.Device? device = null;
        private Task? readTask = null;
        private bool connected = false;

        public bool IsConnected => connected;

        public ErrorCode Connect()
        {
            Debug.Assert(!connected, "Attempted to connect while already connected!");

            var deviceDefinitions = HidApi.Hid.Enumerate(vendorId: VendorId, productId: ProductId).Where(d => d.Usage == Usage);

            if (!deviceDefinitions.Any())
            {
                //No devices were found
                return ErrorCode.NoDevices;
            }

            //Get the device from its definition
            try
            {
                device = deviceDefinitions.First().ConnectToDevice();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                device = null;
            }

            if (device != null)
            {
                if (connected)
                    return ErrorCode.AlreadyConnected;

                connected = true;
                readTask = Task.Run(ReadHIDDevice);
                return ErrorCode.OK;
            }
            else
            {
                return ErrorCode.ErrorConnecting;
            }
        }

        public ErrorCode Disconnect()
        {
            Debug.Assert(connected, "Attempted to disconnect while not connected!");
            if (!connected)
                return ErrorCode.NotConnected;

            connected = false;
            device?.Dispose();
            readTask?.Wait(100);
            readTask?.Dispose();
            device = null;
            return ErrorCode.OK;
        }

        protected abstract void OnReceived(ReadOnlySpan<byte> bytes);

        private void ReadHIDDevice()
        {
            while (connected && device != null)
            {
                try
                {
                    var read = device.Read(MaxPacketLength);
                    OnReceived(read);
                    Task.Delay(1).Wait();
                }
                catch
                {
                    connected = false;
                    return;
                }
            }

            connected = false;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (connected)
                Disconnect();
        }
    }
}
