using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
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

        public void WriteXmlNbt(XElement value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            var type = ParseToNbtType(value.Name.ToString());
            WriteNbtType(type);
            var name = value.Attribute("Name");
            if (!(name is null))
                _writer.Write(name.Value);
            WriteDynamicNbtXml(type, value);
        }

        #region xml write imple methods

        private void WriteInt8ArrayNbtXml(XElement element)
        {
            var elementsArray = element.Elements().ToArray();
            _writer.Write(elementsArray.Length);
            foreach (var value in elementsArray)
                _writer.Write(sbyte.Parse(GetFirstTextContent(value)));
        }

        private void WriteInt32ArrayNbtXml(XElement element)
        {
            var elementsArray = element.Elements().ToArray();
            _writer.Write(elementsArray.Length);
            foreach (var value in elementsArray)
                _writer.Write(int.Parse(GetFirstTextContent(value)));
        }

        private void WriteInt64ArrayNbtXml(XElement element)
        {
            var elementsArray = element.Elements().ToArray();
            _writer.Write(elementsArray.Length);
            foreach (var value in elementsArray)
                _writer.Write(long.Parse(GetFirstTextContent(value)));
        }

        private void WriteListNbtXml(XElement element)
        {
            var contentType = ParseToNbtType(element.Attribute("ContentType").Value);
            WriteNbtType(contentType);
            var elementsArray = element.Elements().ToArray();
            _writer.Write(elementsArray.Length);
            foreach (var value in elementsArray)
                WriteDynamicNbtXml(contentType, value);
        }

        private void WriteCompoundNbtXml(XElement element)
        {
            var elementsArray = element.Elements().ToArray();
            foreach (var child in elementsArray)
            {
                var type = ParseToNbtType(child.Name.ToString());
                if (type == NbtType.TEnd)
                    throw new InvalidDataException();
                WriteNbtType(type);
                _writer.Write(child.Attribute("Name").Value);
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
            var firstTextValue = new Lazy<string>(() => GetFirstTextContent(element));
            switch (type)
            {
                case NbtType.TInt8: _writer.Write(sbyte.Parse(firstTextValue.Value)); break;
                case NbtType.TInt16: _writer.Write(short.Parse(firstTextValue.Value)); break;
                case NbtType.TInt32: _writer.Write(int.Parse(firstTextValue.Value)); break;
                case NbtType.TInt64: _writer.Write(long.Parse(firstTextValue.Value)); break;
                case NbtType.TFloat32: _writer.Write(float.Parse(firstTextValue.Value)); break;
                case NbtType.TFloat64: _writer.Write(double.Parse(firstTextValue.Value)); break;
                case NbtType.TInt8Array: WriteInt8ArrayNbtXml(element); break;
                case NbtType.TString: _writer.Write(firstTextValue.Value); break;
                case NbtType.TList: WriteListNbtXml(element); break;
                case NbtType.TCompound: WriteCompoundNbtXml(element); break;
                case NbtType.TInt32Array: WriteInt32ArrayNbtXml(element); break;
                case NbtType.TInt64Array: WriteInt64ArrayNbtXml(element); break;
            }
        }

        private static string GetFirstTextContent(XElement element)
        {
            return element.Nodes().OfType<XText>().First().Value;
        }

        private static NbtType ParseToNbtType(string value)
        {
            return (NbtType)Enum.Parse(typeof(NbtType), value);
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
