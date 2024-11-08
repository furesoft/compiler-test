using System.Collections.Immutable;
using Silverfly.Nodes;

namespace TestCompiler.Nodes;

public record TupleNode(ImmutableList<AstNode> Values) : AstNode;