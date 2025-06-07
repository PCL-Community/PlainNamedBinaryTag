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

        public XElement ReadNbtAsXml(out NbtType resultType, bool hasName = true)
        {
            resultType = ReadNbtType();
            var result = new XElement(resultType.ToString());
            if (hasName)
                result.Add(new XAttribute("Name", _reader.ReadString()));
            ReadDynamicIntoXml(resultType, result);
            return result;
        }

        #region xml read impl methods

        private void ReadInt8ArrayIntoXml(XElement element)
        {
            var length = _reader.ReadInt32();
            for (int i = 0; i < length; i++)
                element.Add(new XElement(NbtType.TInt8.ToString(), _reader.ReadSByte()));
        }

        private void ReadInt32ArrayIntoXml(XElement element)
        {
            var length = _reader.ReadInt32();
            for (int i = 0; i < length; i++)
                element.Add(new XElement(NbtType.TInt32.ToString(), _reader.ReadInt32()));
        }

        private void ReadInt64ArrayIntoXml(XElement element)
        {
            var length = _reader.ReadInt32();
            for (int i = 0; i < length; i++)
                element.Add(new XElement(NbtType.TInt64.ToString(), _reader.ReadInt64()));
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

        private NbtType ReadNbtType()
        {
            var typedResult = (NbtType)_reader.ReadByte();
            if (!Enum.IsDefined(typeof(NbtType), typedResult))
                throw new InvalidDataException();
            return typedResult;
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
