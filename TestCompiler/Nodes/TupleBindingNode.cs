using System.Collections.Immutable;
using Silverfly.Nodes;

namespace TestCompiler.Nodes;

public record TupleBindingNode(ImmutableList<NameNode> Names, AstNode Value) : AnnotatedNode;