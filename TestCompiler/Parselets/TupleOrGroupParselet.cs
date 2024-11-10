using Silverfly;
using Silverfly.Nodes;
using Silverfly.Parselets;
using TestCompiler.Nodes;

namespace TestCompiler.Parselets;

public class TupleOrGroupParselet : IPrefixParselet
{
    public AstNode Parse(Parser parser, Token token)
    {
        var values = parser.ParseSeperated(",", ")");

        if (values.Count == 1) return new GroupNode("(", ")", values[0]).WithRange(token, parser.LookAhead());

        return new TupleNode(values).WithRange(token, parser.LookAhead());
    }
}