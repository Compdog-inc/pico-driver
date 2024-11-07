using System.Runtime.InteropServices;

namespace DriverStation
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Header
    {
        public byte session;
        public ulong timestamp;

        const int size = 9;

        public Header(byte session, ulong timestamp) { this.session = session; this.timestamp = timestamp; }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[size];
            int index = 0;
            bytes[index++] = session;
            Array.Copy(BitConverter.GetBytes(timestamp), 0, bytes, index, 8);
            index += 8;
            return bytes;
        }
    }

    public class SerializedPacket
    {
        public Header Header { get;set; }

        public SerializedPacket(Header header) { Header = header; }

        public void Serialize(ref byte[] bytes)
        {
            byte[] header = Header.GetBytes();
            byte[] output = new byte[bytes.Length + header.Length];
            Array.Copy(header, 0, output, 0, header.Length);
            Array.Copy(bytes, 0, output, header.Length, bytes.Length);
            bytes = output;
        }
    }

    public static class PacketExtensions
    {
        public static byte[] SerializeWith(this byte[] bytes, SerializedPacket packet)
        {
            packet.Serialize(ref bytes);
            return bytes;
        }
    }
}
