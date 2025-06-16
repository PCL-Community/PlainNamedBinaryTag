using System;
using System.IO;

namespace PlainNamedBinaryTag.Utils
{
    public static class Checker
    {
        /// <summary>
        /// Determines whether the specified stream is in GZip format by checking the magic number header
        /// </summary>
        /// <param name="stream">The stream to check. Must be readable and seekable</param>
        /// <returns>
        /// <c>true</c> if the stream starts with the GZip magic number (0x1F 0x8B);<br/>
        /// otherwise, <c>false</c>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/></exception>
        /// <exception cref="InvalidOperationException">The stream is not readable or not seekable</exception>
        /// <exception cref="IOException">Fail to operate on the stream</exception>
        public static bool IsStreamInGzipFormat(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new InvalidOperationException("Stream must be readable");
            if (!stream.CanSeek)
                throw new InvalidOperationException("Stream must be seekable");

            try
            {
                var curStreamPos = stream.Position;
                var buffer = new byte[2];
                _ = stream.Read(buffer, 0, 2);
                stream.Seek(curStreamPos, SeekOrigin.Begin);
                return buffer[0] == 0x1f && buffer[1] == 0x8b;
            }
            catch (Exception ex)
            {
                throw new IOException("Fail to check GZip header format", ex);
            }
        }
    }
}
