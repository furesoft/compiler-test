using Silverfly;
using Silverfly.Nodes;
using Silverfly.Parselets;
using TestCompiler.Nodes;

namespace TestCompiler.Parselets;

public class ModuleParselet : IPrefixParselet
{
    public AstNode Parse(Parser parser, Token token)
    {
        var arg = parser.ParseExpression();

        AstNode node = new InvalidNode(token);
        if (arg is NameNode name) node = new ModuleNode(name.Token.Text.ToString());

        return node.WithRange(token, parser.LookAhead());
    }
}