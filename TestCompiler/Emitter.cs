using DistIL.AsmIO;
using DistIL.CodeGen.Cil;
using DistIL.IR;
using Silverfly.Nodes;
using Silverfly.Nodes.Operators;

namespace TestCompiler;

public static class Emitter
{
    public static MethodBody Emit(AstNode node, MethodDef method)
    {
        var builder = new MethodBody(method);

        var entryBlock = builder.CreateBlock();

        var firstNode = ((BlockNode)node).Children[0];
        var result = Emit(firstNode, entryBlock);

        if (result != null && result.ResultType != PrimType.Void)
        {
            entryBlock.InsertLast(new ReturnInst(result));
        }
        else
        {
            entryBlock.InsertLast(new ReturnInst());
        }

        return builder;
    }

    private static Value? Emit(AstNode node, BasicBlock block)
    {
        return node switch
        {
            LiteralNode literal => EmitLiteral(literal),
            BinaryOperatorNode bin => EmitBinary(bin, block),
            CallNode call => EmitCall(call, block),
            GroupNode group when group.LeftSymbol == "(" => EmitGroup(group, block),
            _ => null
        };
    }

    private static Value? EmitGroup(GroupNode group, BasicBlock block)
    {
        return Emit(group.Expr, block);
    }

    private static Value EmitLiteral(LiteralNode node)
    {
        Value result = ConstNull.Create();

        switch (node.Value)
        {
            case ulong u:
                result = ConstInt.CreateL((long)u);
                break;
            case long l:
                result = ConstInt.CreateL(l);
                break;
            case int i:
                result = ConstInt.CreateI(i);
                break;
            case short s:
                result = ConstInt.CreateI(s);
                break;
            case byte b:
                result = ConstInt.CreateI(b);
                break;
            case float f:
                result = ConstFloat.CreateS(f);
                break;
            case double d:
                result = ConstFloat.CreateD(d);
                break;
            case bool boolean:
                result = ConstInt.CreateI(boolean ? 1 : 0);
                break;
            case char c:
                result = ConstInt.CreateI(c);
                break;
            case string str:
                result = ConstString.Create(str);
                break;
            default:
                throw new NotSupportedException($"The type '{node.Value.GetType()}' is not supported.");
        }

        return result;
    }

    private static Value EmitCall(CallNode call, BasicBlock block)
    {
        var moduleResolver = block.Method.Definition.DeclaringType.Module.Resolver;

        var consoleType = moduleResolver.Import(typeof(Console));

        Instruction result = null;
        if (call.FunctionExpr is NameNode n && n.Token.ToString() is "print")
        {
            var value = Emit(call.Arguments[0], block);
            var writeLine = consoleType.FindMethod("WriteLine",
                new MethodSig(moduleResolver.SysTypes.Void, [new TypeSig(value.ResultType)]));

            result = new CallInst(writeLine, [value]);
        }

        if (result is not null)
        {
            block.InsertLast(result);
        }

        return result;
    }


    private static Value EmitBinary(BinaryOperatorNode node, BasicBlock block)
    {
        var op = MapBinOperator(node.Operator.Text.ToString());

        var left = Emit(node.LeftExpr, block);
        var right = Emit(node.RightExpr, block);

        var instr = new BinaryInst(op, left, right);

        block.InsertLast(instr);

        return instr;
    }

    private static BinaryOp MapBinOperator(string op) =>
        op switch
        {
            "+" => BinaryOp.Add,
            "-" => BinaryOp.Sub,
            "*" => BinaryOp.Mul,
            "/" => BinaryOp.FDiv,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
}