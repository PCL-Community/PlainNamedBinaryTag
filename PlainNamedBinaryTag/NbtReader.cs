using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using PlainNamedBinaryTag.Utils;

namespace PlainNamedBinaryTag
{
    /// <summary>
    /// Visual Basic cannot perform overload resolution between methods
    /// that accept a <see langword="bool"/> parameter and
    /// those that accept an <see langword="out"/> <see langword="bool"/> parameter,
    /// so let's add a compatibility layer...
    /// </summary>
    public static class VbNbtReaderCreator
    {
        public static NbtReader FromPath(string path, bool compressed) =>
            new NbtReader(path, compressed);

        public static NbtReader FromPathAutoDetect(string path, out bool compressed) =>
            new NbtReader(path, out compressed);

        public static NbtReader FromStream(Stream stream, bool compressed) =>
            new NbtReader(stream, compressed);

        public static NbtReader FromStreamAutoDetect(Stream stream, out bool compressed) =>
            new NbtReader(stream, out compressed);
    }

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

            try
            {
                InitReader(new FileStream(path, FileMode.Open), compressed);
            }
            catch (Exception ex)
            {
                throw new IOException("Fail to create nbt input file stream", ex);
            }
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

            try
            {
                var stream = new FileStream(path, FileMode.Open);
                compressed = Checker.IsStreamInGzipFormat(stream);
                InitReader(stream, compressed);
            }
            catch (Exception ex)
            {
                throw new IOException("Fail to create nbt input file stream", ex);
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="NbtReader"/> class from a stream
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <param name="compressed">Whether to decompress the stream content using GZip</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/></exception>
        /// <exception cref="InvalidOperationException">The stream is not readable</exception>
        public NbtReader(Stream stream, bool compressed)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            // Validate the stream's read functionality,
            // or ArgumentException will be thrown when creating the BinaryReader / GZipStream
            if (!stream.CanRead)
                throw new InvalidOperationException("The input stream must be readable");

            InitReader(stream, compressed);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="NbtReader"/> class from a stream
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <param name="compressed">Output parameter indicating whether the stream is GZip-compressed</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/></exception>
        /// <exception cref="InvalidOperationException">The stream is not readable</exception>
        /// <exception cref="IOException">Fail to check GZip header format</exception>
        public NbtReader(Stream stream, out bool compressed)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            // Throws InvalidOperationException and IOException
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
                // It throws an InvalidDataException here
                // if source is not in gzip format, but we're using a decompress stream
                resultType = _reader.ReadNbtType();

                if (resultType == NbtType.TEnd)
                    throw new InvalidDataException("Cannot read 'end' tag to XML tree");
                
                // Read the root element's type & name
                var result = new XElement(resultType.ToString());
                if (hasName)
                    result.Add(new XAttribute("Name", _reader.ReadString()));

                // Read the root element's payload
                ReadDynamicIntoXml(resultType, result);

                return result;
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException("Unexpected end of NBT stream", ex);
            }
        }

        /// <summary>
        /// Read NBT stream and convert it into NbtNode tree
        /// </summary>
        /// <param name="hasName">
        /// Whether to read the root tag's name<br/>
        /// Note: Legal NBT files always use empty string as root tag name
        /// </param>
        /// <returns>An <see cref="IEnumerable{NbtNode}"/> contains result nbt nodes</returns>
        public IEnumerable<NbtNode> ReadNbtAsNode(bool hasName = true) => ReadNbtAsNode(NodeFilter.None, hasName);

        /// <summary>
        /// Read NBT stream and convert it into NbtNode tree with a filter <br/>
        /// Create filters via <see cref="NodeFilter"/> helper
        /// </summary>
        /// <param name="nodeFilter">The filter to test each NbtNode</param>
        /// <param name="hasName">
        /// Whether to read the root tag's name<br/>
        /// Note: Legal NBT files always use empty string as root tag name
        /// </param>
        /// <returns>An <see cref="IEnumerable{NbtNode}"/> contains result nbt nodes</returns>
        /// <exception cref="ArgumentNullException"><paramref name="nodeFilter"/> is <see langword="null"/></exception>
        public IEnumerable<NbtNode> ReadNbtAsNode(NodeFilterDelegate nodeFilter, bool hasName = true)
        {
            if (nodeFilter is null)
                throw new ArgumentNullException(nameof(nodeFilter));

            // The stack contains all parents of the current node
            var parents = new List<NbtContainerNode>();
            NbtNode currentNode = NbtNode.Create(_reader.ReadNbtType());
            if (hasName)
                currentNode.Name = _reader.ReadString();
            currentNode.ReadPayloadMetadata(_reader);
            // Call the filter to the root node
            var filterResult = nodeFilter.Invoke(parents, currentNode);
            while (true)
            {
                switch (filterResult)
                {
                    case NodeFilterResult.Accept:
                        currentNode.ReadAllContents(_reader);
                        yield return currentNode;
                        break;
                    case NodeFilterResult.TestChildren when currentNode is NbtContainerNode container:
                        if (container.TryGetNextSubNode(_reader, out var subNode))
                        {
                            // Push the current node into stack and work on its next sub node
                            subNode.ReadPayloadMetadata(_reader);
                            parents.Add(container);
                            currentNode = subNode;
                            // Call the filter to the sub node
                            filterResult = nodeFilter.Invoke(parents, currentNode);
                            continue;
                        }
                        break;
                    default: // Ignore, or TestChildren when current node is not container
                        currentNode.SkipAllContents(_reader);
                        break;
                }
                if (parents.Count == 0)
                    yield break;
                // Pop a parent node from the stack and work on its other children
                currentNode = parents[parents.Count - 1];
                parents.RemoveAt(parents.Count - 1);
                filterResult = NodeFilterResult.TestChildren;
            }
        }

        #region xml read impl methods

        private void ReadInt8ArrayIntoXml(XElement element)
        {
            var length = _reader.ReadInt32();
            for (int i = 0; i < length; i++)
                element.Add(new XElement(nameof(NbtType.TInt8), XmlConvert.ToString(_reader.ReadSByte())));
        }

        private void ReadInt32ArrayIntoXml(XElement element)
        {
            var length = _reader.ReadInt32();
            for (int i = 0; i < length; i++)
                element.Add(new XElement(nameof(NbtType.TInt32), XmlConvert.ToString(_reader.ReadInt32())));
        }

        private void ReadInt64ArrayIntoXml(XElement element)
        {
            var length = _reader.ReadInt32();
            for (int i = 0; i < length; i++)
                element.Add(new XElement(nameof(NbtType.TInt64), XmlConvert.ToString(_reader.ReadInt64())));
        }

        private void ReadListIntoXml(XElement element)
        {
            var type = _reader.ReadNbtType();
            element.Add(new XAttribute("ContentType", type));
            var length = _reader.ReadInt32();

            // Content type of empty list tag will be TEnd
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
            
            // Every element in compound tag has an unique name
            var entryNames = new HashSet<string>();
            
            while ((type = _reader.ReadNbtType()) != NbtType.TEnd)
            {
                var child = new XElement(type.ToString());
                
                // Read child's name and write it into 'Name' attribute
                var name = _reader.ReadString();
                if (!entryNames.Add(name))
                    throw new InvalidDataException($"Duplicate name '{name}' in compound tag");
                child.Add(new XAttribute("Name", name));
                
                // Read child's payload
                ReadDynamicIntoXml(type, child);
                
                element.Add(child);
            }
        }

        private void ReadDynamicIntoXml(NbtType type, XElement element)
        {
            // Read payload of specified tag type
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