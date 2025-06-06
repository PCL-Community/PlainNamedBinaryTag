using System;
using System.IO;
using System.IO.Compression;
using PlainNamedBinaryTag.Utils;

namespace PlainNamedBinaryTag
{
    public class NbtReader : IDisposable
    {
        private Stream _stream;

        public NbtReader(string path, ref bool? compressed)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("NBT binary file is not found");
            }
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            if (compressed == null)
            {
                compressed = Checker.IsStreamInGzipFormat(fs);
            }
            if ((bool)compressed)
            {
                _stream = new GZipStream(fs, CompressionMode.Decompress);
            }
            else
            {
                _stream = fs;
            }
        }

        public NbtReader(Stream stream, ref bool? compressed)
        {
            if (compressed == null)
            {
                compressed = Checker.IsStreamInGzipFormat(stream);
            }
            if ((bool)compressed)
            {
                _stream = new GZipStream(stream, CompressionMode.Decompress);
            }
            else
            {
                _stream = stream;
            }
        }

        public object ReadNbt(out string resultName, out NbtType resultType, bool hasName = true)
        {
            resultType = ReadNbtType();
            resultName = hasName ? ReadStringPayload() : null;
            return ReadDynamicPayload(resultType);
        }

        #region "read impl methods"

        private sbyte ReadInt8Payload()
        {
            var result = _stream.ReadByte();
            if (result == -1)
                throw new EndOfStreamException();
            return (sbyte)result;
        }

        private short ReadInt16Payload()
        {
            return ReadBigEndianNumber(2, BitConverter.ToInt16);
        }

        private int ReadInt32Payload()
        {
            return ReadBigEndianNumber(4, BitConverter.ToInt32);
        }

        private long ReadInt64Payload()
        {
            return ReadBigEndianNumber(8, BitConverter.ToInt64);
        }

        private float ReadFloat32Payload()
        {
            return ReadBigEndianNumber(4, BitConverter.ToSingle);
        }

        private double ReadFloat64Payload()
        {
            return ReadBigEndianNumber(8, BitConverter.ToDouble);
        }

        private string ReadStringPayload()
        {
            var length = ReadBigEndianNumber(2, BitConverter.ToUInt16);
            var buffer = new byte[length];
            if (_stream.Read(buffer, 0, length) != length)
                throw new EndOfStreamException();
            return JvmModifiedUtf8.GetString(buffer);
        }

        private NbtInt8Array ReadInt8ArrayPayload()
        {
            var length = ReadInt32Payload();
            var result = new NbtInt8Array(length);
            for (int i = 0; i < length; i++)
                result.Add(ReadInt8Payload());
            return result;
        }

        private NbtInt32Array ReadInt32ArrayPayload()
        {
            var length = ReadInt32Payload();
            var result = new NbtInt32Array(length);
            for (int i = 0; i < length; i++)
                result.Add(ReadInt32Payload());
            return result;
        }

        private NbtInt64Array ReadInt64ArrayPayload()
        {
            var length = ReadInt32Payload();
            var result = new NbtInt64Array(length);
            for (int i = 0; i < length; i++)
                result.Add(ReadInt64Payload());
            return result;
        }

        private NbtList ReadListPayload(out NbtType type)
        {
            type = ReadNbtType();
            var length = ReadInt32Payload();
            var result = new NbtList(type, length);
            for (int i = 0; i < length; i++)
                result.AddWithNbtTypeCheck(ReadDynamicPayload(type));
            return result;
        }

        private NbtCompound ReadCompoundPayload()
        {
            NbtType type;
            var result = new NbtCompound();
            while ((type = ReadNbtType()) != NbtType.TEnd)
                result.Add(ReadStringPayload(), new TypedNbtObject(type, ReadDynamicPayload(type)));
            return result;
        }

        private NbtType ReadNbtType()
        {
            var result = _stream.ReadByte();
            if (result == -1)
                throw new EndOfStreamException();
            var typedResult = (NbtType)result;
            if (!Enum.IsDefined(typeof(NbtType), typedResult))
                throw new InvalidDataException();
            return typedResult;
        }

        private object ReadDynamicPayload(NbtType type)
        {
            switch (type)
            {
                case NbtType.TInt8: return ReadInt8Payload();
                case NbtType.TInt16: return ReadInt16Payload();
                case NbtType.TInt32: return ReadInt32Payload();
                case NbtType.TInt64: return ReadInt64Payload();
                case NbtType.TFloat32: return ReadFloat32Payload();
                case NbtType.TFloat64: return ReadFloat64Payload();
                case NbtType.TInt8Array: return ReadInt8ArrayPayload();
                case NbtType.TString: return ReadStringPayload();
                case NbtType.TList: return ReadListPayload(out _);
                case NbtType.TCompound: return ReadCompoundPayload();
                case NbtType.TInt32Array: return ReadInt32ArrayPayload();
                case NbtType.TInt64Array: return ReadInt64ArrayPayload();
                default: throw new InvalidDataException();
            }
        }

        private T ReadBigEndianNumber<T>(int bufferSize, Func<byte[], int, T> BitConverterResultFunc)
        {
            var buffer = new byte[bufferSize];
            if (_stream.Read(buffer, 0, bufferSize) != bufferSize)
                throw new EndOfStreamException();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);
            return BitConverterResultFunc.Invoke(buffer, 0);
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
