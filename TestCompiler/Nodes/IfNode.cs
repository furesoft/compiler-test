using Silverfly.Nodes;

namespace TestCompiler.Nodes;

public record IfNode(AstNode Condition, AstNode TruePart, AstNode FalsePart) : AstNode;