using System.Runtime.InteropServices;

namespace DriverStation
{
    internal static class SerializedPacket
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Header
        {
            public byte session;
            public uint timestamp;

            public Header(byte session, uint timestamp) { this.session = session; this.timestamp = timestamp; }
        }

        const int size = 5;

        public static byte[] writeHeader(Header header)
        {
            byte[] bytes = new byte[size];
            int index = 0;
            bytes[index++] = header.session;
            Array.Copy(BitConverter.GetBytes(header.timestamp), 0, bytes, index, 4);
            index += 4;
            return bytes;
        }

        public static void injectHeader(ref byte[] bytes, Header header)
        {
            byte[] output = new byte[bytes.Length + size];
            Array.Copy(writeHeader(header), 0, output, 0, size);
            Array.Copy(bytes, 0, output, size, bytes.Length);
            bytes = output;
        }
    }

    internal static class PacketExtensions
    {
        public static byte[] injectHeader(this byte[] bytes, SerializedPacket.Header header)
        {
            SerializedPacket.injectHeader(ref bytes, header);
            return bytes;
        }
    }
}
