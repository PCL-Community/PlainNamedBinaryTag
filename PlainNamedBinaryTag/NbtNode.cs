using System;
using System.Collections.Generic;
using System.IO;
using PlainNamedBinaryTag.Utils;

namespace PlainNamedBinaryTag
{
    #region BaseClasses

    /// <summary>
    /// The base class of all the nbt nodes,
    /// with property <see cref="Name"/> and <see cref="NodeType"/>. <br/>
    /// Use subclasses of <see cref="NbtValueNode{TValue}"/>
    /// to access content value of type sbyte / string / int[] / ... <br/>
    /// Use subclasses of <see cref="NbtContainerNode"/>
    /// to access sub nbt nodes in the tree.
    /// </summary>
    public abstract class NbtNode
    {
        public NbtType NodeType { get; }
        public string Name { get; internal set; } // nullable

        protected NbtNode(NbtType nodeType)
        {
            NodeType = nodeType;
        }

        internal abstract void ReadPayloadMetadata(NbtBinaryReader reader);

        internal abstract void SkipAllContents(NbtBinaryReader reader);

        internal abstract void ReadAllContents(NbtBinaryReader reader);

        internal static NbtNode Create(NbtType type)
        {
            switch (type)
            {
                case NbtType.TEnd: throw new InvalidDataException();
                case NbtType.TInt8: return new NbtInt8Node();
                case NbtType.TInt16: return new NbtInt16Node();
                case NbtType.TInt32: return new NbtInt32Node();
                case NbtType.TInt64: return new NbtInt64Node();
                case NbtType.TFloat32: return new NbtFloat32Node();
                case NbtType.TFloat64: return new NbtFloat64Node();
                case NbtType.TString: return new NbtStringNode();
                case NbtType.TInt8Array: return new NbtInt8ArrayNode();
                case NbtType.TInt32Array: return new NbtInt32ArrayNode();
                case NbtType.TInt64Array: return new NbtInt64ArrayNode();
                case NbtType.TList: return new NbtListNode();
                case NbtType.TCompound: return new NbtCompoundNode();
                default: throw new InvalidDataException();
            }
        }

        internal static void SeekForward(Stream stream, long offset)
        {
            if (offset <= 0)
                return;
            if (stream.CanSeek)
            {
                stream.Seek(offset, SeekOrigin.Current);
                return;
            }
            var bufferLength = (int)Math.Min(offset, 1024 * 1024);
            var buffer = new byte[bufferLength];
            while (offset > 0)
            {
                var read = stream.Read(buffer, 0, (int)Math.Min(bufferLength, offset));
                if (read == 0)
                    throw new EndOfStreamException();
                offset -= read;
            }
        }
    }

    public abstract class NbtValueNode<TValue> : NbtNode
    {
        protected NbtValueNode(NbtType nodeType) : base(nodeType) { }
        public TValue Value { get; protected set; }
        internal sealed override void ReadPayloadMetadata(NbtBinaryReader reader) { }
    }

    public abstract class NbtArrayNode<TValue> : NbtValueNode<TValue[]>
    {
        protected NbtArrayNode(NbtType nodeType) : base(nodeType) { }

        protected abstract int ChildSize { get; }

        protected abstract TValue ReadOneContent(NbtBinaryReader reader);

        internal sealed override void SkipAllContents(NbtBinaryReader reader)
        {
            var length = reader.ReadInt32();
            SeekForward(reader.BaseStream, (long)length * ChildSize);
        }

        internal sealed override void ReadAllContents(NbtBinaryReader reader)
        {
            Value = new TValue[reader.ReadInt32()];
            for (int i = 0; i < Value.Length; i++)
                Value[i] = ReadOneContent(reader);
        }
    }

    public abstract class NbtContainerNode : NbtNode
    {
        protected NbtContainerNode(NbtType nodeType) : base(nodeType) { }
        internal abstract bool TryGetNextSubNode(NbtBinaryReader reader, out NbtNode subNode);
    }

    #endregion

    #region Primitives

    public sealed class NbtInt8Node : NbtValueNode<sbyte>
    {
        internal NbtInt8Node() : base(NbtType.TInt8) { }
        internal override void SkipAllContents(NbtBinaryReader reader) => SeekForward(reader.BaseStream, 1);
        internal override void ReadAllContents(NbtBinaryReader reader) => Value = reader.ReadSByte();
    }

    public sealed class NbtInt16Node : NbtValueNode<short>
    {
        internal NbtInt16Node() : base(NbtType.TInt16) { }
        internal override void SkipAllContents(NbtBinaryReader reader) => SeekForward(reader.BaseStream, 2);
        internal override void ReadAllContents(NbtBinaryReader reader) => Value = reader.ReadInt16();
    }

    public sealed class NbtInt32Node : NbtValueNode<int>
    {
        internal NbtInt32Node() : base(NbtType.TInt32) { }
        internal override void SkipAllContents(NbtBinaryReader reader) => SeekForward(reader.BaseStream, 4);
        internal override void ReadAllContents(NbtBinaryReader reader) => Value = reader.ReadInt32();
    }

    public sealed class NbtInt64Node : NbtValueNode<long>
    {
        internal NbtInt64Node() : base(NbtType.TInt64) { }
        internal override void SkipAllContents(NbtBinaryReader reader) => SeekForward(reader.BaseStream, 8);
        internal override void ReadAllContents(NbtBinaryReader reader) => Value = reader.ReadInt64();
    }

    public sealed class NbtFloat32Node : NbtValueNode<float>
    {
        internal NbtFloat32Node() : base(NbtType.TFloat32) { }
        internal override void SkipAllContents(NbtBinaryReader reader) => SeekForward(reader.BaseStream, 4);
        internal override void ReadAllContents(NbtBinaryReader reader) => Value = reader.ReadSingle();
    }

    public sealed class NbtFloat64Node : NbtValueNode<double>
    {
        internal NbtFloat64Node() : base(NbtType.TFloat64) { }
        internal override void SkipAllContents(NbtBinaryReader reader) => SeekForward(reader.BaseStream, 8);
        internal override void ReadAllContents(NbtBinaryReader reader) => Value = reader.ReadDouble();
    }

    public sealed class NbtStringNode : NbtValueNode<string>
    {
        internal NbtStringNode() : base(NbtType.TString) { }

        internal override void SkipAllContents(NbtBinaryReader reader) =>
            SeekForward(reader.BaseStream, reader.ReadUInt16());

        internal override void ReadAllContents(NbtBinaryReader reader) => Value = reader.ReadString();
    }

    #endregion

    #region Arrays

    public sealed class NbtInt8ArrayNode : NbtArrayNode<sbyte>
    {
        internal NbtInt8ArrayNode() : base(NbtType.TInt8Array) { }
        protected override int ChildSize => 1;
        protected override sbyte ReadOneContent(NbtBinaryReader reader) => reader.ReadSByte();
    }

    public sealed class NbtInt32ArrayNode : NbtArrayNode<int>
    {
        internal NbtInt32ArrayNode() : base(NbtType.TInt32Array) { }
        protected override int ChildSize => 4;
        protected override int ReadOneContent(NbtBinaryReader reader) => reader.ReadInt32();
    }

    public sealed class NbtInt64ArrayNode : NbtArrayNode<long>
    {
        internal NbtInt64ArrayNode() : base(NbtType.TInt64Array) { }
        protected override int ChildSize => 8;
        protected override long ReadOneContent(NbtBinaryReader reader) => reader.ReadInt64();
    }

    #endregion

    #region Containers

    public sealed class NbtListNode : NbtContainerNode
    {
        private int _length;
        private NbtNode[] _content = null;
        private int _subNodeReturnedCount = 0;

        internal NbtListNode() : base(NbtType.TList) { }

        public NbtNode[] Content => _content ?? throw new InvalidOperationException("content not available");
        public NbtType ContentType { get; private set; }

        internal override void ReadPayloadMetadata(NbtBinaryReader reader)
        {
            ContentType = reader.ReadNbtType();
            _length = reader.ReadInt32();
        }

        internal override void SkipAllContents(NbtBinaryReader reader)
        {
            if (ContentType == NbtType.TEnd)
                return;
            var contentInstance = NbtNode.Create(ContentType);
            for (int i = 0; i < _length; i++)
            {
                contentInstance.ReadPayloadMetadata(reader);
                contentInstance.SkipAllContents(reader);
            }
        }

        internal override void ReadAllContents(NbtBinaryReader reader)
        {
            _content = new NbtNode[_length];
            if (ContentType == NbtType.TEnd)
                return;
            for (int i = 0; i < _length; i++)
            {
                var node = NbtNode.Create(ContentType);
                node.ReadPayloadMetadata(reader);
                node.ReadAllContents(reader);
                _content[i] = node;
            }
        }

        internal override bool TryGetNextSubNode(NbtBinaryReader reader, out NbtNode subNode)
        {
            if (++_subNodeReturnedCount > _length)
            {
                subNode = default;
                return false;
            }
            subNode = NbtNode.Create(ContentType);
            return true;
        }
    }

    public sealed class NbtCompoundNode : NbtContainerNode
    {
        private Dictionary<string, NbtNode> _content = null;

        internal NbtCompoundNode() : base(NbtType.TCompound) { }

        public IReadOnlyDictionary<string, NbtNode> Content =>
            _content ?? throw new InvalidOperationException("content not available");

        internal override void ReadPayloadMetadata(NbtBinaryReader reader) { }

        internal override void SkipAllContents(NbtBinaryReader reader)
        {
            NbtType nodeType;
            while ((nodeType = reader.ReadNbtType()) != NbtType.TEnd)
            {
                var subNode = NbtNode.Create(nodeType);
                SeekForward(reader.BaseStream, reader.ReadInt16());
                subNode.ReadPayloadMetadata(reader);
                subNode.SkipAllContents(reader);
            }
        }

        internal override void ReadAllContents(NbtBinaryReader reader)
        {
            _content = new Dictionary<string, NbtNode>();
            NbtType nodeType;
            while ((nodeType = reader.ReadNbtType()) != NbtType.TEnd)
            {
                var subNode = NbtNode.Create(nodeType);
                var name = reader.ReadString();
                subNode.Name = name;
                subNode.ReadPayloadMetadata(reader);
                subNode.ReadAllContents(reader);
                try
                {
                    _content.Add(name, subNode);
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidDataException($"Duplicate name '{name}' in compound tag", ex);
                }
            }
        }

        internal override bool TryGetNextSubNode(NbtBinaryReader reader, out NbtNode subNode)
        {
            NbtType nodeType = reader.ReadNbtType();
            if (nodeType == NbtType.TEnd)
            {
                subNode = default;
                return false;
            }
            subNode = NbtNode.Create(nodeType);
            subNode.Name = reader.ReadString();
            return true;
        }
    }

    #endregion
}