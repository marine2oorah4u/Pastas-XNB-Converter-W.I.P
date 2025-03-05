using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XNBConverter
{
    public static class XNBStructures
    {
        public const byte ContentCompressedLzx = 0x80;
        public const byte ContentCompressedLz4 = 0x40;
        public const byte HiDefContent = 0x01;
        public const byte XnbFormatVersion = 5;

        public enum SurfaceFormat
        {
            Color = 0,
            Bgr565 = 1,
            Bgra5551 = 2,
            Bgra4444 = 3,
            Dxt1 = 4,
            Dxt3 = 5,
            Dxt5 = 6,
            NormalizedByte2 = 7,
            NormalizedByte4 = 8,
            Rgba1010102 = 9,
            Rg32 = 10,
            Rgba64 = 11,
            Alpha8 = 12,
            Single = 13,
            Vector2 = 14,
            Vector4 = 15,
            HalfSingle = 16,
            HalfVector2 = 17,
            HalfVector4 = 18,
            HdrBlendable = 19
        }

        public enum AudioFormat
        {
            Pcm = 1,
            Adpcm = 2,
            WmaPro = 3,
            Wma = 4,
            XmaPro = 5,
            Xma2 = 6
        }

        public static readonly char[] TargetPlatformIdentifiers = new[]
        {
        'w', // Windows
        'x', // Xbox 360
        'i', // iOS
        'a', // Android
        'd', // DesktopGL
        'X', // MacOSX
        'n', // NativeClient
        'r', // RaspberryPi
        'P', // PlayStation4
        '5', // PlayStation5
        'O', // XboxOne
        'S', // Nintendo Switch
        'b', // WebAssembly
        'V', // DesktopVK
        'G', // Windows GDK
        's'  // Xbox Series
    };
    }

    // ContentTypeReader.cs
    public abstract class ContentTypeReader
    {
        public abstract object Read(BinaryReader reader);
        public virtual bool CanDeserializeIntoExistingObject => false;
        public virtual int TypeVersion => 0;
    }

    // Specific type readers will inherit from this
    public class ContentTypeReader<T> : ContentTypeReader
    {
        public override object Read(BinaryReader reader)
        {
            return ReadAsset(reader);
        }

        protected virtual T ReadAsset(BinaryReader reader)
        {
            throw new NotImplementedException();
        }
    }


}
