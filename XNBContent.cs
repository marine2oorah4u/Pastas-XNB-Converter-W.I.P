using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XNBConverter
{
    public class XNBContent
    {
        public int Format { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int MipCount { get; set; }
        public byte[] Data { get; set; }
    }
}
