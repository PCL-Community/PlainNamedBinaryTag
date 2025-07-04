using System;
using System.IO;
using System.Text;

namespace PlainNamedBinaryTag.Utils
{
    /// <summary>
    /// Provides big-endian number and jvm-modified utf8 string (with ushort length prefix) read functionality
    /// </summary>
    public class NbtBinaryReader : BinaryReader
    {
        private const int BufferCapacity = 8;

        private byte[] _buffer = new byte[BufferCapacity];

        public NbtBinaryReader(Stream input) : base(input) { }

        public NbtBinaryReader(Stream input, Encoding encoding) : base(input, encoding) { }

        public NbtBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }

        /// <summary>NotSupported</summary>
        public override int PeekChar() => throw new NotSupportedException();

        /// <summary>NotSupported</summary>
        public override int Read() => throw new NotSupportedException();

        /// <summary>NotSupported</summary>
        public override int Read(char[] buffer, int index, int count) => throw new NotSupportedException();

        /// <summary>NotSupported</summary>
        public override char ReadChar() => throw new NotSupportedException();

        /// <summary>NotSupported</summary>
        public override char[] ReadChars(int count) => throw new NotSupportedException();

        /// <summary>NotSupported</summary>
        public override decimal ReadDecimal() => throw new NotSupportedException();

        public override bool ReadBoolean()
        {
            FillBuffer(1);
            return _buffer[0] != 0;
        }

        public override byte ReadByte()
        {
            FillBuffer(1);
            return _buffer[0];
        }

        public override sbyte ReadSByte()
        {
            FillBuffer(1);
            return (sbyte)_buffer[0];
        }

        public override ushort ReadUInt16()
        {
            FillBuffer(2);
            return (ushort)(_buffer[0] << 8 | _buffer[1]);
        }

        public override short ReadInt16()
        {
            FillBuffer(2);
            return (short)(_buffer[0] << 8 | _buffer[1]);
        }

        public override int ReadInt32()
        {
            FillBuffer(4);
            return (_buffer[0] << 24) | (_buffer[1] << 16) | (_buffer[2] << 8) | _buffer[3];
        }

        public override uint ReadUInt32()
        {
            FillBuffer(4);
            return (uint)((_buffer[0] << 24) | (_buffer[1] << 16) | (_buffer[2] << 8) | _buffer[3]);
        }

        public override long ReadInt64()
        {
            FillBuffer(8);
            return ((long)_buffer[0] << 56) | ((long)_buffer[1] << 48) | ((long)_buffer[2] << 40) | ((long)_buffer[3] << 32) |
                ((long)_buffer[4] << 24) | ((long)_buffer[5] << 16) | ((long)_buffer[6] << 8) | _buffer[7];
        }

        public override ulong ReadUInt64()
        {
            FillBuffer(8);
            return ((ulong)_buffer[0] << 56) | ((ulong)_buffer[1] << 48) | ((ulong)_buffer[2] << 40) | ((ulong)_buffer[3] << 32) |
                ((ulong)_buffer[4] << 24) | ((ulong)_buffer[5] << 16) | ((ulong)_buffer[6] << 8) | _buffer[7];
        }

        public override float ReadSingle()
        {
            FillBuffer(4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(_buffer, 0, 4);
            return BitConverter.ToSingle(_buffer, 0);
        }

        public override double ReadDouble()
        {
            FillBuffer(8);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(_buffer, 0, 8);
            return BitConverter.ToDouble(_buffer, 0);
        }

        /// <inheritdoc />
        /// <exception cref="InvalidDataException">Fail to decode bytes</exception>
        public override string ReadString()
        {
            var length = ReadUInt16();
            var bytes = ReadBytes(length);
            if (bytes.Length != length)
                throw new EndOfStreamException();
            try
            {
                return JvmModifiedUtf8.GetString(bytes);
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidDataException("Fail to decode jvm-modified utf8 string", ex);
            }
        }

        /// <summary>
        /// Reads a <see cref="NbtType"/> from the current stream
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException">The byte read is not a valid <see cref="NbtType"/> value</exception>
        public NbtType ReadNbtType()
        {
            var result = ReadByte();
            return Enum.IsDefined(typeof(NbtType), result)
                ? (NbtType)result
                : throw new InvalidDataException($"{result:X2} is not a valid {nameof(NbtType)} enum value");
        }

        protected override void FillBuffer(int numBytes)
        {
            if (numBytes <= 0 || numBytes > BufferCapacity)
                throw new ArgumentOutOfRangeException(nameof(numBytes));
            int bytesRead = 0;
            do
            {
                int n = BaseStream.Read(_buffer, bytesRead, numBytes - bytesRead);
                if (n == 0)
                    throw new EndOfStreamException();
                bytesRead += n;
            } while (bytesRead < numBytes);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _buffer = null;
        }
    }
}
