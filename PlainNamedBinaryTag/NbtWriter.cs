using System;
using System.IO;
using System.IO.Compression;
using PlainNamedBinaryTag.Utils;

namespace PlainNamedBinaryTag
{
    public class NbtWriter : IDisposable
    {
        private NbtBinaryWriter _writer;

        public NbtWriter(string path, bool compressed)
        {
            Stream stream;
            if (!File.Exists(path))
            {
                stream = File.Create(path);
            }
            else
            {
                stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
            }
            if (compressed)
            {
                stream = new GZipStream(stream, CompressionMode.Compress);
            }
            _writer = new NbtBinaryWriter(stream);
        }

        public NbtWriter(Stream stream, bool compressed)
        {
            if (compressed)
            {
                stream = new GZipStream(stream, CompressionMode.Compress);
            }
            _writer = new NbtBinaryWriter(stream);
        }

        public void WriteNbt(object value, string name = "", NbtType? specifiedType = null)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            var type = specifiedType ?? NbtTypeMapper.ToNbtType(value.GetType());
            WriteNbtType(type);
            if (name != null)
            {
                _writer.Write(name);
            }
            WriteDynamicPayload(type, value);
        }

        #region "write impl methods"

        private void WriteInt8ArrayPayload(NbtInt8Array array)
        {
            _writer.Write(array.Count);
            foreach (var value in array)
                _writer.Write(value);
        }

        private void WriteInt32ArrayPayload(NbtInt32Array array)
        {
            _writer.Write(array.Count);
            foreach (var value in array)
                _writer.Write(value);
        }

        private void WriteInt64ArrayPayload(NbtInt64Array array)
        {
            _writer.Write(array.Count);
            foreach (var value in array)
                _writer.Write(value);
        }

        private void WriteListPayload(NbtList list)
        {
            WriteNbtType(list.ContentType);
            _writer.Write(list.Count);
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
                _writer.Write(pair.Key);
                WriteDynamicPayload(type, pair.Value.Value);
            }
            WriteNbtType(NbtType.TEnd);
        }

        private void WriteNbtType(NbtType value)
        {
            _writer.Write((byte)value);
        }

        private void WriteDynamicPayload(NbtType type, object value)
        {
            switch (type)
            {
                case NbtType.TInt8: _writer.Write((sbyte)value); break;
                case NbtType.TInt16: _writer.Write((short)value); break;
                case NbtType.TInt32: _writer.Write((int)value); break;
                case NbtType.TInt64: _writer.Write((long)value); break;
                case NbtType.TFloat32: _writer.Write((float)value); break;
                case NbtType.TFloat64: _writer.Write((double)value); break;
                case NbtType.TInt8Array: WriteInt8ArrayPayload((NbtInt8Array)value); break;
                case NbtType.TString: _writer.Write((string)value); break;
                case NbtType.TList: WriteListPayload((NbtList)value); break;
                case NbtType.TCompound: WriteCompoundPayload((NbtCompound)value); break;
                case NbtType.TInt32Array: WriteInt32ArrayPayload((NbtInt32Array)value); break;
                case NbtType.TInt64Array: WriteInt64ArrayPayload((NbtInt64Array)value); break;
                default: throw new InvalidDataException();
            }
        }

        #endregion

        private bool _isDispoed = false;
        public void Dispose()
        {
            if (!_isDispoed)
            {
                _isDispoed = true;
                _writer?.Dispose();
                _writer = null;
            }
        }
    }
}
