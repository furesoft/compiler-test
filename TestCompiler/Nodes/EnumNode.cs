using System.Collections.Immutable;
using Silverfly.Nodes;

namespace TestCompiler.Nodes;

public record EnumNode(string Name, ImmutableList<AstNode> Members) : AstNode
{
}