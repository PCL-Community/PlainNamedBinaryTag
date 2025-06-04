using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PlainNamedBinaryTag.Utils
{
    public static class Checker
    {
        public static bool IsStreamInGzipFormat(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream is null");
            }
            if (!stream.CanRead)
            {
                throw new ArgumentException("The stream is not readable");
            }
            try
            {
                var b1 = stream.ReadByte();
                var b2 = stream.ReadByte();
                stream.Seek(0, SeekOrigin.Begin);
                return b1 == 0x1f && b2 == 0x8b;
            }
            catch
            {
                return false;
            }
        }
    }
}
