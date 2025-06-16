using System;
using System.IO;
using System.Text;

namespace PlainNamedBinaryTag.Utils
{
    public static class JvmModifiedUtf8
    {
        /// <summary>
        /// Encode JvmModifiedUtf8
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
        public static byte[] GetBytes(string value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            using (var ms = new MemoryStream())
            {
                foreach (char c in value)
                {
                    if(c <= 0x7F && c != '\0') // ascii
                    {
                        ms.WriteByte((byte)c);
                    }
                    else if (c <= 0x07FF) // two bytes
                    {
                        ms.WriteByte((byte)(0xC0 | (c >> 6)));
                        ms.WriteByte((byte)(0x80 | (c & 0x3F)));
                    }
                    else
                    {
                        ms.WriteByte((byte)(0xE0 | (c >> 12)));
                        ms.WriteByte((byte)(0x80 | ((c >> 6) & 0x3F)));
                        ms.WriteByte((byte)(0x80 | (c & 0x3F)));
                    }
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Decode JvmModifiedUtf8
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <see langword="null"/></exception>
        /// <exception cref="InvalidDataException">Fail to decode bytes</exception>
        public static string GetString(byte[] bytes)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));
            var result = new StringBuilder(bytes.Length);
            for (int i = 0; i < bytes.Length; i++)
            {
                byte b1 = bytes[i];
                if ((b1 & 0x80) == 0) // one byte: 0b_0xxx_xxxx
                {
                    result.Append((char)b1);
                }
                else if ((b1 & 0xE0) == 0xC0) // two bytes: 0b_110x_xxxx
                {
                    if (i + 1 >= bytes.Length)
                        throw new InvalidDataException("Expected 2-byte sequence but reached end of input");
                    byte b2 = bytes[++i];
                    if ((b2 & 0xC0) != 0x80) // 0b_10xx_xxxx
                        throw new InvalidDataException($"Continuation byte 0x{b2:X2} (at position {i}) does not match 0b_10xx_xxxx pattern");
                    int code = ((b1 & 0x1F) << 6) | (b2 & 0x3F);
                    if (code < 0x80 && code != 0x00)
                        throw new InvalidDataException($"Overlong encoding for code point 0x{code:X4} (at position {i-1})");
                    result.Append((char)code);
                }
                else if ((b1 & 0xF0) == 0xE0) // three bytes: 0b_1110_xxxx
                {
                    if (i + 2 >= bytes.Length)
                        throw new InvalidDataException("Expected 3-byte sequence but reached end of input");
                    byte b2 = bytes[++i];
                    byte b3 = bytes[++i];
                    if ((b2 & 0xC0) != 0x80 || (b3 & 0xC0) != 0x80)
                        throw new InvalidDataException($"Continuation bytes 0x{b2:X2} 0x{b3:X2} (at position {i-1}, {i}) do not match 0b_10xx_xxxx pattern");
                    int code = ((b1 & 0x0F) << 12) | ((b2 & 0x3F) << 6) | (b3 & 0x3F);
                    if (code < 0x0800)
                        throw new InvalidDataException($"Overlong encoding for code point 0x{code:X4} (at position {i - 2})");
                    result.Append((char)code);
                }
                else
                {
                    throw new InvalidDataException($"Leading byte 0x{b1:X2} (at position {i}) was not supported");
                }
            }
            return result.ToString();
        }
    }
}
