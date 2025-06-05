using System;

namespace PlainNamedBinaryTag
{
    public static class NbtTypeMapper
    {
        private static readonly Type[] clrTypes = new Type[] {
            null, typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(float), typeof(double),
            typeof(NbtInt8Array), typeof(string), typeof(NbtList), typeof(NbtCompound), typeof(NbtInt32Array), typeof(NbtInt64Array)
        };

        public static Type ToClrType(NbtType nbtType)
        {
            if (!Enum.IsDefined(typeof(NbtType), nbtType))
                throw new ArgumentOutOfRangeException(nameof(nbtType), nbtType, $"Value ({nbtType}) not defined in {typeof(NbtType)} enum");
            return clrTypes[(int)nbtType];
        }

        public static NbtType ToNbtType(Type clrType)
        {
            var result = Array.IndexOf(clrTypes, clrType);
            if (result == -1)
                throw new ArgumentException($"{clrType} is not a supported clr Type in NbtType conversation", nameof(clrType));
            return (NbtType)result;
        }
    }
}
