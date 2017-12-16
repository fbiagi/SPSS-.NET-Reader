using System;
using System.IO;
using System.Text;

namespace SpssLib.FileParser
{
    public static class Helper
    {
        public static void ReverseArray(byte[] source, byte[] target)
        {
            int len = source.Length;
            for (int i = 0; i < len; i++)
                target[i] = source[len - 1 - i];
        }
    }

    // Binary reader that supports both little-endian and big-endian byte orders. It assumes that the current environment is little-indian.
    public class DualBinaryReader : BinaryReader
    {
        public DualBinaryReader(Stream stream, Encoding encoding) : base(stream, encoding)
        {

        }

        public bool IsLittleEndian { get; set; } = true;

        public override Int16 ReadInt16()
        {
            Int16 i;

            if (this.IsLittleEndian)
                i = base.ReadInt16();
            else
            {
                byte b1 = this.ReadByte();
                byte b0 = this.ReadByte();
                i = (short)(b1 << 8 + b0);
            }

            return i;
        }

        public override UInt16 ReadUInt16()
        {
            UInt16 i;

            if (this.IsLittleEndian)
                i = base.ReadUInt16();
            else
            {
                byte b1 = this.ReadByte();
                byte b0 = this.ReadByte();
                i = (UInt16)(b1 << 8 + b0);
            }

            return i;
        }

        public override int ReadInt32()
        {
            int i;

            if (this.IsLittleEndian)
                i = base.ReadInt32();
            else
            {
                i = this.ReadByte();

                for (int k = 0; k < 3; k++)
                    i = (i << 8) | this.ReadByte();
            }

            return i;
        }

        public override uint ReadUInt32()
        {
            uint i;

            if (this.IsLittleEndian)
                i = base.ReadUInt32();
            else
            {
                i = this.ReadByte();

                for (int k = 0; k < 3; k++)
                    i = (i << 8) | this.ReadByte();
            }

            return i;
        }

        public override double ReadDouble()
        {
            double f;

            if (this.IsLittleEndian)
                f = base.ReadDouble();
            else
            {
                UInt64 l = this.ReadByte();

                for(int k = 0; k < 7; k++)
                    l = (l << 8) | this.ReadByte();

                f = BitConverter.ToDouble( BitConverter.GetBytes(l), 0);
            }

            return f;
        }

    }
}
