using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using PlainNamedBinaryTag.Utils;

namespace PlainNamedBinaryTag
{
    public class NbtReader : IDisposable
    {
        private NbtBinaryReader _reader;

        /// <summary>
        /// Initialize a new instance of NbtReader class with a file path
        /// </summary>
        /// <param name="path">The path of the file to read</param>
        /// <param name="compressed">
        /// Specifies whether to decompress the file content.
        /// Set to null for automatic detection
        /// </param>
        /// <exception cref="FileNotFoundException" />
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

        /// <summary>
        /// Initialize a new instance of NbtReader class with a stream
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <param name="compressed">
        /// Specifies whether to decompress the stream content.
        /// Set to null for automatic detection
        /// </param>
        /// <exception cref="FileNotFoundException" />
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

        /// <summary>
        /// Read NBT stream and convert it into XML structure
        /// </summary>
        /// <param name="resultType">Type of the root tag (will be TCompound if reading a .dat file)</param>
        /// <param name="hasName">
        /// Whether to read the root tag's name<br/>
        /// Note: Legal NBT files always use empty string as root tag name
        /// </param>
        /// <returns>Root XElement of the parsed XML tree</returns>
        /// <exception cref="InvalidDataException" />
        /// <exception cref="IOException" />
        public XElement ReadNbtAsXml(out NbtType resultType, bool hasName = true)
        {
            try
            {
                resultType = ReadNbtType();
                if (resultType == NbtType.TEnd)
                    throw new InvalidDataException("Cannot read 'end' tag to XML tree");
                var result = new XElement(resultType.ToString());
                if (hasName)
                    result.Add(new XAttribute("Name", _reader.ReadString()));
                ReadDynamicIntoXml(resultType, result);
                return result;
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException("Unexpected end of NBT stream", ex);
            }
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
            if (type == NbtType.TEnd && length != 0)
                throw new InvalidDataException("List tag with 'end' content-type cannot contain any child");
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
            var entryNames = new HashSet<string>();
            while ((type = ReadNbtType()) != NbtType.TEnd)
            {
                var child = new XElement(type.ToString());
                var name = _reader.ReadString();
                if (!entryNames.Add(name))
                    throw new InvalidDataException($"Duplicate name '{name}' in compound tag");
                child.Add(new XAttribute("Name", name));
                ReadDynamicIntoXml(type, child);
                element.Add(child);
            }
        }

        private NbtType ReadNbtType()
        {
            var typedResult = (NbtType)_reader.ReadByte();
            if (!Enum.IsDefined(typeof(NbtType), typedResult))
                throw new InvalidDataException($"Invalid NbtType: 0x{(byte)typedResult:X2}");
            return typedResult;
        }

        private void ReadDynamicIntoXml(NbtType type, XElement element)
        {
            switch (type)
            {
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
                default: throw new InvalidDataException($"Invalid NbtType: 0x{(byte)type:X2}");
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
