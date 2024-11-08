using Silverfly.Nodes;

namespace TestCompiler.Nodes;

public record ImportNode(string Path) : AstNode;