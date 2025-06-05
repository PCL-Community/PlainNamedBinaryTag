using System;
using System.Collections.Generic;

namespace PlainNamedBinaryTag
{
    public interface INbtTypedCollection
    {
        NbtType ContentType { get; }
    }

    public class NbtInt8Array : List<sbyte>, INbtTypedCollection
    {
        public NbtType ContentType => NbtType.TInt8;

        public NbtInt8Array() { }

        public NbtInt8Array(int capacity = 0) : base(capacity) { }
    }

    public class NbtInt32Array : List<int>, INbtTypedCollection
    {
        public NbtType ContentType => NbtType.TInt32;

        public NbtInt32Array() { }

        public NbtInt32Array(int capacity) : base(capacity) { }

        public override string ToString()
        {
            return $"{GetType().Name} - [{string.Join(", ", this)}]";
        }
    }

    public class NbtInt64Array : List<long>, INbtTypedCollection
    {
        public NbtType ContentType => NbtType.TInt64;

        public NbtInt64Array() { }

        public NbtInt64Array(int capacity) : base(capacity) { }

        public override string ToString()
        {
            return $"{GetType().Name} - [{string.Join(", ", this)}]";
        }
    }

    public class NbtList : List<object>, INbtTypedCollection
    {
        private readonly NbtType _type;
        private readonly Type _clrType;

        public NbtType ContentType => _type;

        public NbtList(NbtType type) : this(type, 0) { }

        public NbtList(NbtType type, int capacity) : base(capacity)
        {
            _type = type;
            _clrType = NbtTypeMapper.ToClrType(type);
        }

        public void AddWithNbtTypeCheck(object item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (!_clrType.IsAssignableFrom(item.GetType()))
                throw new ArgumentException("Illegal type of item adding into NbtList: " +
                    $"was ({item.GetType()}), should be ({_clrType})");
            Add(item);
        }

        public override string ToString()
        {
            return $"{GetType().Name} - [{string.Join(", ", this)}]";
        }
    }

    public class NbtCompound : Dictionary<string, TypedNbtObject> 
    {
        public override string ToString()
        {
            return $"{GetType().Name} - {string.Join(", ", this)}";
        }
    }

    public struct TypedNbtObject
    {
        public readonly NbtType NbtType;

        public readonly object Value;

        public TypedNbtObject(NbtType nbtType, object value)
        {
            NbtType = nbtType;
            Value = value;
        }

        public override string ToString()
        {
            return $"{NbtType}: {Value}";
        }
    }
}
