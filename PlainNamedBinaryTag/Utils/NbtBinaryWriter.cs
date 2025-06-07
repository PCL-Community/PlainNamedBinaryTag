using System;
using System.IO;
using System.Text;

namespace PlainNamedBinaryTag.Utils
{
    /// <summary>
    /// Provides big-endian number and jvm-modified utf8 string (with ushort length prefix) write functionality
    /// </summary>
    public class NbtBinaryWriter : BinaryWriter
    {
        private const int _bufferCapacity = 8;

        private readonly byte[] _buffer = new byte[_bufferCapacity];

        public NbtBinaryWriter(Stream output) : base(output) { }

        public NbtBinaryWriter(Stream output, Encoding encoding) : base(output, encoding) { }

        public NbtBinaryWriter(Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen) { }

        protected NbtBinaryWriter() { }

        /// <summary>NotSupported</summary>
        public override void Write(char ch) { throw new NotSupportedException(); }

        /// <summary>NotSupported</summary>
        public override void Write(char[] chars) { throw new NotSupportedException(); }

        /// <summary>NotSupported</summary>
        public override void Write(char[] chars, int index, int count) { throw new NotSupportedException(); }

        /// <summary>NotSupported</summary>
        public override void Write(decimal value) { throw new NotSupportedException(); }

        public override void Write(byte[] buffer)
        {
            base.Write(buffer);
        }

        public override void Write(byte[] buffer, int index, int count)
        {
            base.Write(buffer, index, count);
        }

        public override void Write(bool value)
        {
            _buffer[0] = (byte)(value ? 1 : 0);
            OutStream.Write(_buffer, 0, 1);
        }

        public override void Write(byte value)
        {
            _buffer[0] = value;
            OutStream.Write(_buffer, 0, 1);
        }

        public override void Write(sbyte value)
        {
            _buffer[0] = (byte)value;
            OutStream.Write(_buffer, 0, 1);
        }

        public override void Write(short value)
        {
            _buffer[0] = (byte)(value >> 8);
            _buffer[1] = (byte)value;
            OutStream.Write(_buffer, 0, 2);
        }

        public override void Write(ushort value)
        {
            _buffer[0] = (byte)(value >> 8);
            _buffer[1] = (byte)value;
            OutStream.Write(_buffer, 0, 2);
        }

        public override void Write(int value)
        {
            _buffer[0] = (byte)(value >> 24);
            _buffer[1] = (byte)(value >> 16);
            _buffer[2] = (byte)(value >> 8);
            _buffer[3] = (byte)value;
            OutStream.Write(_buffer, 0, 4);
        }

        public override void Write(uint value)
        {
            _buffer[0] = (byte)(value >> 24);
            _buffer[1] = (byte)(value >> 16);
            _buffer[2] = (byte)(value >> 8);
            _buffer[3] = (byte)value;
            OutStream.Write(_buffer, 0, 4);
        }

        public override void Write(long value)
        {
            _buffer[0] = (byte)(value >> 56);
            _buffer[1] = (byte)(value >> 48);
            _buffer[2] = (byte)(value >> 40);
            _buffer[3] = (byte)(value >> 32);
            _buffer[4] = (byte)(value >> 24);
            _buffer[5] = (byte)(value >> 16);
            _buffer[6] = (byte)(value >> 8);
            _buffer[7] = (byte)value;
            OutStream.Write(_buffer, 0, 8);
        }

        public override void Write(ulong value)
        {
            _buffer[0] = (byte)(value >> 56);
            _buffer[1] = (byte)(value >> 48);
            _buffer[2] = (byte)(value >> 40);
            _buffer[3] = (byte)(value >> 32);
            _buffer[4] = (byte)(value >> 24);
            _buffer[5] = (byte)(value >> 16);
            _buffer[6] = (byte)(value >> 8);
            _buffer[7] = (byte)value;
            OutStream.Write(_buffer, 0, 8);
        }

        public override void Write(float value)
        {
            var buffer = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);
            OutStream.Write(buffer, 0, 4);
        }

        public override void Write(double value)
        {
            var buffer = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);
            OutStream.Write(buffer, 0, 8);
        }

        public override void Write(string value)
        {
            var buffer = JvmModifiedUtf8.GetBytes(value);
            if (buffer.Length > ushort.MaxValue)
                throw new ArgumentException("String too long");
            Write((ushort)buffer.Length);
            OutStream.Write(buffer, 0, buffer.Length);
        }
    }
}
