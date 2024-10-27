using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace DriverStation
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct XboxPacket : IEquatable<XboxPacket>
    {
        [Flags]
        public enum Buttons : byte
        {
            A = 1 << 0,
            B = 1 << 1,
            X = 1 << 2,
            Y = 1 << 3,
            LB = 1 << 4,
            RB = 1 << 5,
            _6 = 1 << 6,
            _7 = 1 << 7
        }

        public enum POVAngle : byte
        {
            None = 0,
            Deg_0 = 1,
            Deg_45 = 2,
            Deg_90 = 3,
            Deg_135 = 4,
            Deg_180 = 5,
            Deg_225 = 6,
            Deg_270 = 7,
            Deg_315 = 8
        }

        public Buttons buttons;

        public short axis_X;
        public short axis_Y;
        public short axis_3;
        public short axis_4;
        public POVAngle pov;
        public short axis_LT;
        public short axis_RT;

        const int size = 14;

        private bool empty;
        public bool IsEmpty { get { return empty; } }

        public XboxPacket()
        {
            empty = false;
        }

        private XboxPacket(bool isEmpty)
        {
            empty = isEmpty;
        }

        public static readonly XboxPacket Empty = new XboxPacket(true);

        public static short setAxis(float axis)
        {
            return (short)MathF.Floor(axis * short.MaxValue);
        }

        public int toBytes(byte[] bytes)
        {
            if (bytes.Length >= size)
            {
                int index = 0;
                bytes[index++] = (byte)buttons;
                Array.Copy(BitConverter.GetBytes(axis_X), 0, bytes, index, 2);
                index += 2;
                Array.Copy(BitConverter.GetBytes(axis_Y), 0, bytes, index, 2);
                index += 2;
                Array.Copy(BitConverter.GetBytes(axis_3), 0, bytes, index, 2);
                index += 2;
                Array.Copy(BitConverter.GetBytes(axis_4), 0, bytes, index, 2);
                index += 2;
                bytes[index++] = (byte)pov;
                Array.Copy(BitConverter.GetBytes(axis_LT), 0, bytes, index, 2);
                index += 2;
                Array.Copy(BitConverter.GetBytes(axis_RT), 0, bytes, index, 2);
                index += 2;
                return size;
            }

            return -1;
        }

        public byte[] toBytes()
        {
            byte[] bytes = new byte[size];
            toBytes(bytes);
            return bytes;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null) return false;
            else if (obj is XboxPacket packet)
                return Equals(packet);
            else
                return false;
        }

        public bool Equals(XboxPacket other)
        {
            return
                other.empty == empty &&
                other.buttons == buttons &&
                other.axis_X == axis_X &&
                other.axis_Y == axis_Y &&
                other.axis_3 == axis_3 &&
                other.axis_4 == axis_4 &&
                other.pov == pov &&
                other.axis_LT == axis_LT &&
                other.axis_RT == axis_RT
                ;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(buttons, axis_X, axis_Y, axis_3, axis_4, pov, axis_LT, axis_RT);
        }
    }
}
