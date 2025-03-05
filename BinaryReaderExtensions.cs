using System.IO;

namespace XNBConverter
{
    public static class BinaryReaderExtensions
    {
        public static int Read7BitEncodedInt(this BinaryReader reader)
        {
            int result = 0;
            int shift = 0;

            while (shift < 35) // Max 5 bytes for a 32-bit integer
            {
                byte b = reader.ReadByte();
                result |= (b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                    return result;
                shift += 7;
            }

            throw new FormatException("Too many bytes in what should have been a 7-bit encoded integer.");
        }
    }
}