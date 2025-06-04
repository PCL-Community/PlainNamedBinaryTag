using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlainNamedBinaryTag
{
    public enum NodeType : Byte
    {
        TEnd = 0,
        TByte = 1,
        TInt16 = 2,
        TInt32 = 3,
        TInt64 = 4,
        TFloot = 5,
        TDouble = 6,
        TByteArray = 7,
        TString = 8,
        TList = 9,
        TNode = 10,
        TInt32Array = 11,
        TInt64Array =12,
    }
}
