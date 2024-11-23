namespace DriverStation.Providers
{
    public class CircuitPythonJoystickProvider : HIDDeviceProvider
    {
        protected override ushort VendorId => 0x239A;
        protected override ushort ProductId => 0x80F4;
        protected override ushort Usage => 5;

        private float X = 0;
        private float Y = 0;

        public event Action? Updated;

        public override XboxPacket Packet
        {
            get
            {
                XboxPacket packet = new XboxPacket();
                packet.axis_X = XboxPacket.setAxis(-X);
                packet.axis_Y = XboxPacket.setAxis(-Y);
                return packet;
            }
        }

        private short prevX;
        private short prevY;

        protected override void OnReceived(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 7)
            {
                short x = BitConverter.ToInt16(bytes[3..]);
                short y = BitConverter.ToInt16(bytes[5..]);
                if (Math.Abs(x) > 300 || Math.Abs(y) > 300)
                {
                    X = (float)x / short.MaxValue;
                    Y = (float)y / short.MaxValue;
                    if (x != prevX || y != prevY)
                    {
                        prevX = x; prevY = y;
                        Updated?.Invoke();
                    }
                }
                else
                {
                    X = 0;
                    Y = 0;
                }
            }
        }
    }
}
