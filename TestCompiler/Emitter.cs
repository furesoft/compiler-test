using DistIL.AsmIO;
using DistIL.CodeGen.Cil;
using DistIL.IR;
using DistIL.IR.Utils;
using Silverfly.Nodes;
using Silverfly.Nodes.Operators;
using TestCompiler.Nodes;

namespace TestCompiler;

public static class Emitter
{
    public static MethodBody Emit(AstNode node, MethodDef method)
    {
        method.Body = new MethodBody(method);

        var entryBlock = method.Body.CreateBlock();
        var builder = new DistIL.IR.Utils.IRBuilder(entryBlock);

        Value? result = null;
        for (var index = 0; index < ((BlockNode)node).Children.Count; index++)
        {
            var child = ((BlockNode)node).Children[index];
            result = Emit(child, builder);
        }

        if (result != null && result.ResultType != PrimType.Void)
        {
            entryBlock.InsertLast(new ReturnInst(result));
        }
        else
        {
            entryBlock.InsertLast(new ReturnInst());
        }

        return method.Body;
    }

    private static Value? Emit(AstNode node, IRBuilder block)
    {
        return node switch
        {
            LiteralNode literal => EmitLiteral(literal),
            BinaryOperatorNode bin => EmitBinary(bin, block),
            CallNode call => EmitCall(call, block),
            GroupNode group when group.LeftSymbol == "(" => EmitGroup(group, block),
            VariableBindingNode let => EmitVariableBinding(let, block),
            NameNode name => EmitName(name, block),
            _ => null
        };
    }

    private static Value? EmitName(NameNode name, IRBuilder builder)
    {
        if (_variables.TryGetValue(name.Token.ToString(), out var variable))
        {
            return builder.CreateLoad(variable);
        }

        return null;
    }

    static Dictionary<string, LocalSlot> _variables = new();
    private static Value? EmitVariableBinding(VariableBindingNode let, IRBuilder builder)
    {
        var result = Emit(let.Value, builder);
        var variable = builder.Method.Definition.Body.CreateVar(result.ResultType, let.Name.ToString());

        _variables.Add(let.Name.ToString(), variable);

        return builder.CreateStore(variable, result);
    }

    private static Value? EmitGroup(GroupNode group, IRBuilder builder)
    {
        return Emit(group.Expr, builder);
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

    private static Value EmitCall(CallNode call, IRBuilder builder)
    {
        var moduleResolver = builder.Method.Definition.DeclaringType.Module.Resolver;

        var consoleType = moduleResolver.Import(typeof(Console));

        if (call.FunctionExpr is NameNode n && n.Token.ToString() is "print")
        {
            var value = Emit(call.Arguments[0], builder);

            TypeDesc valueType = value.ResultType;
            if (valueType is CompoundType c)
            {
                valueType = c.ElemType;
            }

            var writeLine = consoleType.FindMethod("WriteLine",
                new MethodSig(moduleResolver.SysTypes.Void, [new TypeSig(valueType)]));

            return builder.CreateCall(writeLine, value);
        }

        return null;
    }


    private static Value EmitBinary(BinaryOperatorNode node, IRBuilder builder)
    {
        var op = MapBinOperator(node.Operator.Text.ToString());

        var left = Emit(node.LeftExpr, builder);
        var right = Emit(node.RightExpr, builder);

        return builder.CreateBin(op, left, right);
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