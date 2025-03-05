using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XNBConverter
{
    public class XNBHeader
    {
        public char[] Identifier { get; set; } = new char[3]; // "XNB"
        public byte Platform { get; set; }
        public byte Version { get; set; }
        public byte Flags { get; set; }
        public uint FileSize { get; set; }
        public bool IsCompressed => (Flags & 0x80) != 0;
        public bool IsHiDef => (Flags & 0x01) != 0;

        public void Read(BinaryReader reader)
        {
            Identifier = reader.ReadChars(3);
            Platform = reader.ReadByte();
            Version = reader.ReadByte();
            Flags = reader.ReadByte();
            FileSize = reader.ReadUInt32();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Identifier);
            writer.Write(Platform);
            writer.Write(Version);
            writer.Write(Flags);
            writer.Write(FileSize);
        }
    }
}
