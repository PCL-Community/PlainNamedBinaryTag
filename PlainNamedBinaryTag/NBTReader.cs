using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PlainNamedBinaryTag.Utils;

namespace PlainNamedBinaryTag
{
    public class NBTReader : IDisposable
    {
        private Stream _stream;
        public NBTReader(string path, bool isCompressed = false)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("NBT binary file is not found");
            }
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (Checker.IsStreamInGzipFormat(fs))
            {
                _stream = new GZipStream(fs, CompressionMode.Decompress);
            }
            else
            {
                _stream = fs;
            }
        }

        private bool _isDispoed = false;
        public void Dispose()
        {
            if (!_isDispoed)
            {
                _isDispoed = true;
                _stream?.Dispose();
                _stream = null;
            }
        }
    }
}
