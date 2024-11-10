using System.Collections.Immutable;
using Silverfly;
using Silverfly.Nodes;

namespace TestCompiler.Nodes;

public record VariableBindingNode(Token Name, ImmutableList<NameNode> Parameters, AstNode Value) : AnnotatedNode
{
}