using System;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using PlainNamedBinaryTag.Utils;

namespace PlainNamedBinaryTag
{
    public class NbtReader : IDisposable
    {
        private NbtBinaryReader _reader;

        public NbtReader(string path, ref bool? compressed)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("NBT binary file is not found");
            }
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            if (compressed == null)
            {
                compressed = Checker.IsStreamInGzipFormat(stream);
            }
            if ((bool)compressed)
            {
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }
            _reader = new NbtBinaryReader(stream);
        }

        public NbtReader(Stream stream, ref bool? compressed)
        {
            if (compressed == null)
            {
                compressed = Checker.IsStreamInGzipFormat(stream);
            }
            if ((bool)compressed)
            {
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }
            _reader = new NbtBinaryReader(stream);
        }

        public object ReadNbt(out string resultName, out NbtType resultType, bool hasName = true)
        {
            resultType = ReadNbtType();
            resultName = hasName ? _reader.ReadString() : null;
            return ReadDynamicPayload(resultType);
        }

        public XElement ReadNbtAsXml(out NbtType resultType, bool hasName = true)
        {
            var result = new XElement((resultType = ReadNbtType()).ToString());
            if (hasName)
                result.Add(new XAttribute("Name", _reader.ReadString()));
            ReadDynamicIntoXml(resultType, result);
            return result;
        }

        #region "read impl methods"

        private NbtInt8Array ReadInt8ArrayPayload()
        {
            var length = _reader.ReadInt32();
            var result = new NbtInt8Array(length);
            for (int i = 0; i < length; i++)
                result.Add(_reader.ReadSByte());
            return result;
        }

        private NbtInt32Array ReadInt32ArrayPayload()
        {
            var length = _reader.ReadInt32();
            var result = new NbtInt32Array(length);
            for (int i = 0; i < length; i++)
                result.Add(_reader.ReadInt32());
            return result;
        }

        private NbtInt64Array ReadInt64ArrayPayload()
        {
            var length = _reader.ReadInt32();
            var result = new NbtInt64Array(length);
            for (int i = 0; i < length; i++)
                result.Add(_reader.ReadInt64());
            return result;
        }

        private NbtList ReadListPayload()
        {
            var type = ReadNbtType();
            var length = _reader.ReadInt32();
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
                result.Add(_reader.ReadString(), new TypedNbtObject(type, ReadDynamicPayload(type)));
            return result;
        }

        private NbtType ReadNbtType()
        {
            var typedResult = (NbtType)_reader.ReadByte();
            if (!Enum.IsDefined(typeof(NbtType), typedResult))
                throw new InvalidDataException();
            return typedResult;
        }

        private object ReadDynamicPayload(NbtType type)
        {
            switch (type)
            {
                case NbtType.TInt8: return _reader.ReadSByte();
                case NbtType.TInt16: return _reader.ReadInt16();
                case NbtType.TInt32: return _reader.ReadInt32();
                case NbtType.TInt64: return _reader.ReadInt64();
                case NbtType.TFloat32: return _reader.ReadSingle();
                case NbtType.TFloat64: return _reader.ReadDouble();
                case NbtType.TInt8Array: return ReadInt8ArrayPayload();
                case NbtType.TString: return _reader.ReadString();
                case NbtType.TList: return ReadListPayload();
                case NbtType.TCompound: return ReadCompoundPayload();
                case NbtType.TInt32Array: return ReadInt32ArrayPayload();
                case NbtType.TInt64Array: return ReadInt64ArrayPayload();
                default: throw new InvalidDataException();
            }
        }

        #endregion

        #region xml read impl methods

        private void ReadInt8ArrayIntoXml(XElement element)
        {
            var length = _reader.ReadInt32();
            for (int i = 0; i < length; i++)
                element.Add(_reader.ReadSByte());
        }

        private void ReadInt32ArrayIntoXml(XElement element)
        {
            var length = _reader.ReadInt32();
            for (int i = 0; i < length; i++)
                element.Add(_reader.ReadInt32());
        }

        private void ReadInt64ArrayIntoXml(XElement element)
        {
            var length = _reader.ReadInt32();
            for (int i = 0; i < length; i++)
                element.Add(_reader.ReadInt64());
        }

        private void ReadListIntoXml(XElement element)
        {
            var type = ReadNbtType();
            element.Add(new XAttribute("ContentType", type));
            var length = _reader.ReadInt32();
            for (int i = 0; i < length; i++)
            {
                var child = new XElement(type.ToString());
                ReadDynamicIntoXml(type, child);
                element.Add(child);
            }
        }

        private void ReadCompoundIntoXml(XElement element)
        {
            NbtType type;
            while ((type = ReadNbtType()) != NbtType.TEnd)
            {
                var child = new XElement(type.ToString());
                child.Add(new XAttribute("Name", _reader.ReadString()));
                ReadDynamicIntoXml(type, child);
                element.Add(child);
            }
        }

        private void ReadDynamicIntoXml(NbtType type, XElement element)
        {
            switch (type) {
                case NbtType.TInt8: element.Add(_reader.ReadSByte()); break;
                case NbtType.TInt16: element.Add(_reader.ReadInt16()); break;
                case NbtType.TInt32: element.Add(_reader.ReadInt32()); break;
                case NbtType.TInt64: element.Add(_reader.ReadInt64()); break;
                case NbtType.TFloat32: element.Add(_reader.ReadSingle()); break;
                case NbtType.TFloat64: element.Add(_reader.ReadDouble()); break;
                case NbtType.TInt8Array: ReadInt8ArrayIntoXml(element); break;
                case NbtType.TString: element.Add(_reader.ReadString()); break;
                case NbtType.TList: ReadListIntoXml(element); break;
                case NbtType.TCompound: ReadCompoundIntoXml(element); break;
                case NbtType.TInt32Array: ReadInt32ArrayIntoXml(element); break;
                case NbtType.TInt64Array: ReadInt64ArrayIntoXml(element); break;
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
                _reader?.Dispose();
                _reader = null;
            }
        }
    }
}
