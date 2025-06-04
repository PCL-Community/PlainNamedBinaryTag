using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlainNamedBinaryTag.NodeModel
{
    public class BaseNode
    {
        public NodeType ID { get; set; }
        public string Name { get; set; }
        public object Load { get; set; }
    }
}
