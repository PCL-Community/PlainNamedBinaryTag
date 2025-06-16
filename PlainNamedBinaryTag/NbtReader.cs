using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using PlainNamedBinaryTag.Utils;

namespace PlainNamedBinaryTag
{
    public class NbtReader : IDisposable
    {
        private NbtBinaryReader _reader;

        /// <summary>
        /// Initializes a new instance of the <see cref="NbtReader"/> class from a file path
        /// </summary>
        /// <param name="path">The path of the file to read</param>
        /// <param name="compressed">Whether to decompress the file content using GZip</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/></exception>
        /// <exception cref="FileNotFoundException">The specified file is not found</exception>
        /// <exception cref="IOException">Fail to create input file stream</exception>
        public NbtReader(string path, bool compressed)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException("NBT binary file is not found");

            Stream stream;
            try
            {
                stream = new FileStream(path, FileMode.Open);
            }
            catch (Exception ex)
            {
                throw new IOException("Fail to create nbt input file stream", ex);
            }
            InitReader(stream, compressed);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NbtReader"/> class from a file path
        /// </summary>
        /// <param name="path">The path of the file to read</param>
        /// <param name="compressed">Output parameter indicating whether the file is GZip-compressed</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/></exception>
        /// <exception cref="FileNotFoundException">The specified file is not found</exception>
        /// <exception cref="IOException">Fail to create input file stream</exception>
        public NbtReader(string path, out bool compressed)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException("NBT binary file is not found");

            Stream stream;
            try
            {
                stream = new FileStream(path, FileMode.Open);
                compressed = Checker.IsStreamInGzipFormat(stream);
            }
            catch (Exception ex)
            {
                throw new IOException("Fail to create nbt input file stream", ex);
            }
            InitReader(stream, compressed);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="NbtReader"/> class from a stream
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <param name="compressed">Whether to decompress the stream content using GZip</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/></exception>
        public NbtReader(Stream stream, bool compressed)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            InitReader(stream, compressed);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="NbtReader"/> class from a stream
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <param name="compressed">Output parameter indicating whether the stream is GZip-compressed</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/></exception>
        /// <exception cref="InvalidOperationException">The stream is not readable</exception>
        /// <exception cref="IOException">Failed to check GZip header format</exception>
        public NbtReader(Stream stream, out bool compressed)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            compressed = Checker.IsStreamInGzipFormat(stream);
            InitReader(stream, compressed);
        }

        private void InitReader(Stream stream, bool compressed)
        {
            if (compressed)
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
        /// <exception cref="InvalidDataException">Nbt binary data is invalid</exception>
        /// <exception cref="IOException">An I/O error occurs during reading the source stream</exception>
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
                element.Add(new XElement(NbtType.TInt8.ToString(), XmlConvert.ToString(_reader.ReadSByte())));
        }

        private void ReadInt32ArrayIntoXml(XElement element)
        {
            var length = _reader.ReadInt32();
            for (int i = 0; i < length; i++)
                element.Add(new XElement(NbtType.TInt32.ToString(), XmlConvert.ToString(_reader.ReadInt32())));
        }

        private void ReadInt64ArrayIntoXml(XElement element)
        {
            var length = _reader.ReadInt32();
            for (int i = 0; i < length; i++)
                element.Add(new XElement(NbtType.TInt64.ToString(), XmlConvert.ToString(_reader.ReadInt64())));
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
                case NbtType.TEnd: throw new InvalidDataException("Unexpected TEnd, this is an internal error");
                case NbtType.TInt8: element.Add(XmlConvert.ToString(_reader.ReadSByte())); break;
                case NbtType.TInt16: element.Add(XmlConvert.ToString(_reader.ReadInt16())); break;
                case NbtType.TInt32: element.Add(XmlConvert.ToString(_reader.ReadInt32())); break;
                case NbtType.TInt64: element.Add(XmlConvert.ToString(_reader.ReadInt64())); break;
                case NbtType.TFloat32: element.Add(XmlConvert.ToString(_reader.ReadSingle())); break;
                case NbtType.TFloat64: element.Add(XmlConvert.ToString(_reader.ReadDouble())); break;
                case NbtType.TInt8Array: ReadInt8ArrayIntoXml(element); break;
                case NbtType.TString: element.Add(_reader.ReadString()); break;
                case NbtType.TList: ReadListIntoXml(element); break;
                case NbtType.TCompound: ReadCompoundIntoXml(element); break;
                case NbtType.TInt32Array: ReadInt32ArrayIntoXml(element); break;
                case NbtType.TInt64Array: ReadInt64ArrayIntoXml(element); break;
                default: throw new InvalidDataException($"Invalid NbtType: 0x{(byte)type:X2}, this is an internal error");
            }
        }

        #endregion

        private bool _isDisposed;
        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            _reader?.Dispose();
            _reader = null;
        }
    }
}
