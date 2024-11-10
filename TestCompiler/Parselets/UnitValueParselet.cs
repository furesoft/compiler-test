using Silverfly;
using Silverfly.Nodes;
using Silverfly.Parselets;

namespace TestCompiler.Parselets;

public class UnitValueParselet : IPrefixParselet
{
    public AstNode Parse(Parser parser, Token token)
    {
        return new LiteralNode(null, token).WithRange(token);
    }
}