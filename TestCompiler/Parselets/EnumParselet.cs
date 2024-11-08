using Silverfly;
using Silverfly.Nodes;
using Silverfly.Parselets;
using TestCompiler.Nodes;

namespace TestCompiler.Parselets;

public class EnumParselet : IPrefixParselet
{
    public AstNode Parse(Parser parser, Token token)
    {
        var name = parser.Consume(PredefinedSymbols.Name);

        parser.Consume("=");
        var members = parser.ParseSeperated("|", bindingPower: 0, PredefinedSymbols.EOL);

        return new EnumNode(name.Text.ToString(), members).WithRange(token, parser.LookAhead());
    }
}
