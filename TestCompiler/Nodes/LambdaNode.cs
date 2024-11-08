using System.Collections.Immutable;
using Silverfly.Nodes;

namespace TestCompiler.Nodes;

public record LambdaNode(ImmutableList<NameNode> Parameters, AstNode Value) : AstNode
{
}
