using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using PlainNamedBinaryTag.Utils;

namespace PlainNamedBinaryTag
{
    public class NbtWriter : IDisposable
    {
        private NbtBinaryWriter _writer;

        /// <summary>
        /// Initializes a new instance of <see cref="NbtWriter"/> class from a file path
        /// </summary>
        /// <param name="path">The path of the file to write</param>
        /// <param name="compressed">Whether to compress the file content using GZip</param>
        /// <exception cref="IOException">Fail to create output file stream</exception>
        public NbtWriter(string path, bool compressed)
        {
            Stream stream;
            try
            {
                stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            }
            catch (Exception ex)
            {
                throw new IOException("Fail to create nbt output file stream", ex);
            }
            if (compressed)
            {
                stream = new GZipStream(stream, CompressionMode.Compress);
            }
            _writer = new NbtBinaryWriter(stream);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="NbtWriter"/> class from a stream
        /// </summary>
        /// <param name="stream">The stream to write</param>
        /// <param name="compressed">Whether to compress the stream content using GZip</param>
        public NbtWriter(Stream stream, bool compressed)
        {
            if (compressed)
            {
                stream = new GZipStream(stream, CompressionMode.Compress);
            }
            _writer = new NbtBinaryWriter(stream);
        }

        /// <summary>
        /// Convert XML into NBT and write it into the stream
        /// </summary>
        /// <param name="value">The root XElement of XML tree</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
        /// <exception cref="FormatException">Xml data is invalid. Such as missing some attributes</exception>
        /// <exception cref="IOException">An I/O error occurs during writing the destination stream</exception>
        public void WriteXmlNbt(XElement value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var type = ParseToNbtType(value.Name.LocalName);
            if (type == NbtType.TEnd)
                throw new FormatException("End tag cannot be add into XML tree, don't instantiate it");
            WriteNbtType(type);
            if (value.Attribute("Name") is XAttribute attr)
                _writer.Write(attr.Value);
            WriteDynamicNbtXml(type, value);
        }

        #region xml write impl methods

        private void WriteInt8ArrayNbtXml(XElement element)
        {
            var children = element.Elements().ToArray();
            _writer.Write(children.Length);
            foreach (var child in children)
            {
                if (child.Name.LocalName != NbtType.TInt8.ToString())
                    throw new FormatException($"Illegal content of Int8Array: {child}");
                try
                {
                    _writer.Write(XmlConvert.ToSByte(child.Value));
                }
                catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException)
                {
                    throw new FormatException($"Invalid content of {child.Name.LocalName} tag: '{child.Value}'", ex);
                }
            }
        }

        private void WriteInt32ArrayNbtXml(XElement element)
        {
            var children = element.Elements().ToArray();
            _writer.Write(children.Length);
            foreach (var child in children)
            {
                if (child.Name.LocalName != NbtType.TInt32.ToString())
                    throw new FormatException($"Illegal content of Int32Array: {child}");
                try
                {
                    _writer.Write(XmlConvert.ToInt32(child.Value));
                }
                catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException)
                {
                    throw new FormatException($"Invalid content of {child.Name.LocalName} tag: '{child.Value}'", ex);
                }
            }
        }

        private void WriteInt64ArrayNbtXml(XElement element)
        {
            var children = element.Elements().ToArray();
            _writer.Write(children.Length);
            foreach (var child in children)
            {
                if (child.Name.LocalName != NbtType.TInt64.ToString())
                    throw new FormatException($"Illegal content of Int64Array: {child}");
                try
                {
                    _writer.Write(XmlConvert.ToInt64(child.Value));
                }
                catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException)
                {
                    throw new FormatException($"Invalid content of {child.Name.LocalName} tag: '{child.Value}'", ex);
                }
            }
        }

        private void WriteListNbtXml(XElement element)
        {
            NbtType contentType;
            if (element.Attribute("ContentType") is XAttribute attr)
                contentType = ParseToNbtType(attr.Value);
            else
                throw new FormatException("List tag must have 'ContentType' attribute represents the NbtType of its contents");
            var children = element.Elements().ToArray();
            if (contentType == NbtType.TEnd && children.Count() != 0)
                throw new FormatException("List tag with 'end' content-type cannot contain any child");
            WriteNbtType(contentType);
            _writer.Write(children.Length);
            foreach (var child in children)
            {
                if (child.Name.LocalName != contentType.ToString())
                    throw new FormatException("List tag can only contain content of the same type");
                WriteDynamicNbtXml(contentType, child);
            }
        }

        private void WriteCompoundNbtXml(XElement element)
        {
            var entryNames = new HashSet<string>();
            foreach (var child in element.Elements())
            {
                var type = ParseToNbtType(child.Name.LocalName);
                if (type == NbtType.TEnd)
                    throw new FormatException("End tag cannot be add into XML tree, don't instantiate it");
                WriteNbtType(type);
                if (child.Attribute("Name") is XAttribute attr)
                {
                    var name = attr.Value;
                    if (!entryNames.Add(name))
                        throw new FormatException($"Duplicate name '{name}' in compound tag");
                    _writer.Write(name);
                }
                else
                    throw new FormatException("Tags in compound tag must have 'Name' attribute");
                WriteDynamicNbtXml(type, child);
            }
            WriteNbtType(NbtType.TEnd);
        }

        private void WriteNbtType(NbtType value)
        {
            _writer.Write((byte)value);
        }

        private void WriteDynamicNbtXml(NbtType type, XElement element)
        {
            switch (type)
            {
                case NbtType.TInt8Array: WriteInt8ArrayNbtXml(element); return;
                case NbtType.TList: WriteListNbtXml(element); return;
                case NbtType.TCompound: WriteCompoundNbtXml(element); return;
                case NbtType.TInt32Array: WriteInt32ArrayNbtXml(element); return;
                case NbtType.TInt64Array: WriteInt64ArrayNbtXml(element); return;
            }
            string content = element.Value;
            try
            {
                switch (type)
                {
                    case NbtType.TInt8: _writer.Write(XmlConvert.ToSByte(content)); return;
                    case NbtType.TInt16: _writer.Write(XmlConvert.ToInt16(content)); return;
                    case NbtType.TInt32: _writer.Write(XmlConvert.ToInt32(content)); return;
                    case NbtType.TInt64: _writer.Write(XmlConvert.ToInt64(content)); return;
                    case NbtType.TFloat32: _writer.Write(XmlConvert.ToSingle(content)); return;
                    case NbtType.TFloat64: _writer.Write(XmlConvert.ToDouble(content)); return;
                    case NbtType.TString: _writer.Write(content); return;
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException)
            {
                throw new FormatException($"Invalid content of {type} tag: '{content}'", ex);
            }
            throw new FormatException($"Invalid NbtType: 0x{(byte)type:X2}");
        }

        private static NbtType ParseToNbtType(string value)
        {
            return Enum.TryParse(value, out NbtType result)
                ? result
                : throw new FormatException($"Invalid NbtType string: '{value}'");
        }

        #endregion

        private bool _isDisposed = false;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _writer?.Dispose();
                _writer = null;
            }
        }
    }
}
