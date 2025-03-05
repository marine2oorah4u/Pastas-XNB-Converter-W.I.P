using System.IO;

namespace XNBConverter
{
    public static class BinaryWriterExtensions
    {
        public static void Write7BitEncodedInt(this BinaryWriter writer, int value)
        {
            uint v = (uint)value;
            while (v >= 0x80)
            {
                writer.Write((byte)(v | 0x80));
                v >>= 7;
            }
            writer.Write((byte)v);
        }
    }
}
