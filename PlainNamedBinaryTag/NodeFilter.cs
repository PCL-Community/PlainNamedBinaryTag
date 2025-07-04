using System.Collections.Generic;

namespace PlainNamedBinaryTag
{
    public delegate NodeFilterResult NodeFilterDelegate(IReadOnlyList<NbtContainerNode> parents, NbtNode currentNode);

    public enum NodeFilterResult
    {
        Ignore,
        Accept,
        TestChildren
    }

    public static class NodeFilter
    {
        public static NodeFilterDelegate None => (_, __) => NodeFilterResult.Accept;

        public static NodeFilterDelegate MatchAbsPath(params string[] path)
        {
            return (parents, node) =>
            {
                if (path.Length < parents.Count + 1)
                    return NodeFilterResult.Ignore; // unexpected
                for (var i = 0; i < parents.Count; i++)
                {
                    if (parents[i].Name != path[i])
                        return NodeFilterResult.Ignore;
                }
                if (path[parents.Count] == node.Name)
                    return parents.Count + 1 == path.Length ? NodeFilterResult.Accept : NodeFilterResult.TestChildren;
                return NodeFilterResult.Ignore;
            };
        }

        public static NodeFilterDelegate MatchNameAnywhere(string name)
        {
            return (parents, node) =>
                node.Name == name ? NodeFilterResult.Accept : NodeFilterResult.TestChildren;
        }
    }
}