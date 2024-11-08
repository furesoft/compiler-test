using Silverfly;
using Silverfly.Nodes;
using Silverfly.Parselets;
using TestCompiler.Nodes;

namespace TestCompiler.Parselets;

public class ImportParselet : IPrefixParselet
{
    public AstNode Parse(Parser parser, Token token)
    {
        var arg = parser.ParseExpression();

        AstNode node = new InvalidNode(token);
        if (arg is LiteralNode { Value: string path })
        {
            node = new ImportNode(path);
        }
        else if (arg is NameNode name)
        {
            node = new ImportNode(name.Token.Text.ToString());
        }

        return node.WithRange(token, parser.LookAhead());
    }
}
