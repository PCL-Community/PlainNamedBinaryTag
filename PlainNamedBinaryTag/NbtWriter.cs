using System;
using System.IO;
using System.IO.Compression;
using PlainNamedBinaryTag.Utils;

namespace PlainNamedBinaryTag
{
    public class NbtWriter : IDisposable
    {
        private Stream _stream;

        public NbtWriter(string path, bool compressed)
        {
            FileStream fs;
            if (!File.Exists(path))
            {
                fs = File.Create(path);
            }
            else
            {
                fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
            }
            if (compressed)
            {
                _stream = new GZipStream(fs, CompressionMode.Compress);
            }
            else
            {
                _stream = fs;
            }
        }

        public NbtWriter(Stream stream, bool compressed)
        {
            if (compressed)
            {
                _stream = new GZipStream(stream, CompressionMode.Compress);
            }
            else
            {
                _stream = stream;
            }
        }

        public void WriteNbt(object value, string name = "", NbtType? specifiedType = null)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            var type = specifiedType ?? NbtTypeMapper.ToNbtType(value.GetType());
            WriteNbtType(type);
            if (name != null)
            {
                WriteStringPayload(name);
            }
            WriteDynamicPayload(type, value);
        }

        #region "write impl methods"

        private void WriteInt8Payload(sbyte value)
        {
            _stream.WriteByte((byte)value);
        }

        private void WriteInt16Payload(short value)
        {
            WriteBigEndianNumber(value, BitConverter.GetBytes);
        }

        private void WriteInt32Payload(int value)
        {
            WriteBigEndianNumber(value, BitConverter.GetBytes);
        }

        private void WriteInt64Payload(long value)
        {
            WriteBigEndianNumber(value, BitConverter.GetBytes);
        }

        private void WriteFloat32Payload(float value)
        {
            WriteBigEndianNumber(value, BitConverter.GetBytes);
        }

        private void WriteFloat64Payload(double value)
        {
            WriteBigEndianNumber(value, BitConverter.GetBytes);
        }

        private void WriteStringPayload(string value)
        {
            var buffer = JvmModifiedUtf8.GetBytes(value);
            if (buffer.Length > ushort.MaxValue)
                throw new ArgumentException("String too long", nameof(value));
            WriteBigEndianNumber((ushort)buffer.Length, BitConverter.GetBytes);
            _stream.Write(buffer, 0, buffer.Length);
        }

        private void WriteInt8ArrayPayload(NbtInt8Array array)
        {
            WriteInt32Payload(array.Count);
            foreach (var value in array)
                WriteInt8Payload(value);
        }

        private void WriteInt32ArrayPayload(NbtInt32Array array)
        {
            WriteInt32Payload(array.Count);
            foreach (var value in array)
                WriteInt32Payload(value);
        }

        private void WriteInt64ArrayPayload(NbtInt64Array array)
        {
            WriteInt32Payload(array.Count);
            foreach (var value in array)
                WriteInt64Payload(value);
        }

        private void WriteListPayload(NbtList list)
        {
            WriteNbtType(list.ContentType);
            WriteInt32Payload(list.Count);
            foreach (var value in list)
                WriteDynamicPayload(list.ContentType, value);
        }

        private void WriteCompoundPayload(NbtCompound compound)
        {
            foreach (var pair in compound)
            {
                var type = pair.Value.NbtType;
                if (type == NbtType.TEnd)
                    throw new InvalidDataException();
                WriteNbtType(type);
                WriteStringPayload(pair.Key);
                WriteDynamicPayload(type, pair.Value.Value);
            }
            WriteNbtType(NbtType.TEnd);
        }

        private void WriteNbtType(NbtType value)
        {
            _stream.WriteByte((byte)value);
        }

        private void WriteDynamicPayload(NbtType type, object value)
        {
            switch (type)
            {
                case NbtType.TInt8: WriteInt8Payload((sbyte)value); break;
                case NbtType.TInt16: WriteInt16Payload((short)value); break;
                case NbtType.TInt32: WriteInt32Payload((int)value); break;
                case NbtType.TInt64: WriteInt64Payload((long)value); break;
                case NbtType.TFloat32: WriteFloat32Payload((float)value); break;
                case NbtType.TFloat64: WriteFloat64Payload((double)value); break;
                case NbtType.TInt8Array: WriteInt8ArrayPayload((NbtInt8Array)value); break;
                case NbtType.TString: WriteStringPayload((string)value); break;
                case NbtType.TList: WriteListPayload((NbtList)value); break;
                case NbtType.TCompound: WriteCompoundPayload((NbtCompound)value); break;
                case NbtType.TInt32Array: WriteInt32ArrayPayload((NbtInt32Array)value); break;
                case NbtType.TInt64Array: WriteInt64ArrayPayload((NbtInt64Array)value); break;
                default: throw new InvalidDataException();
            }
        }

        private void WriteBigEndianNumber<T>(T value, Func<T, byte[]> BitConverterBufferFunc)
        {
            var buffer = BitConverterBufferFunc.Invoke(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);
            _stream.Write(buffer, 0, buffer.Length);
        }

        #endregion

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
