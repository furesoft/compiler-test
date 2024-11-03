using DistIL.AsmIO;
using DistIL.CodeGen.Cil;
using DistIL.IR;
using Silverfly.Nodes;
using Silverfly.Nodes.Operators;

namespace TestCompiler;

public static class Emitter
{
    public static void Emit(ModuleResolver moduleResolver, AstNode node, MethodDef method)
    {
        var builder = new MethodBody(method);

        var consoleType = moduleResolver.Import(typeof(Console));
        var writeLine = consoleType.FindMethod("WriteLine",
            new MethodSig(moduleResolver.SysTypes.Void, [new TypeSig(PrimType.Int64)]));

        var entryBlock = builder.CreateBlock();

        var firstNode = ((BlockNode)node).Children[0];
        var result = ToValue(firstNode, entryBlock);

        entryBlock.InsertLast(new CallInst(writeLine, [result]));

        entryBlock.InsertLast(new ReturnInst());

        method.Body = builder;
        method.ILBody = ILGenerator.GenerateCode(method.Body);
    }

    private static Value? ToValue(AstNode node, BasicBlock block)
    {
        return node switch
        {
            LiteralNode literal => ToValue(literal, block),
            BinaryOperatorNode bin => ToValue(bin, block),
            _ => null
        };
    }

    private static Value ToValue(LiteralNode node, BasicBlock block)
    {
        Value result = ConstNull.Create();

        if (node.Value is ulong u)
        {
            result = ConstInt.CreateL((long)u);
        }

        return result;
    }


    private static Value ToValue(BinaryOperatorNode node, BasicBlock block)
    {
        var op = MapOperator(node.Operator.Text.ToString());

        var left = ToValue(node.LeftExpr, block);
        var right = ToValue(node.RightExpr, block);

        var instr = new BinaryInst(op, left, right);

        block.InsertLast(instr);

        return instr;
    }

    private static BinaryOp MapOperator(string op)
    {
        return op switch
        {
            "+" => BinaryOp.Add,
            "-" => BinaryOp.Sub,
            "*" => BinaryOp.Mul,
            "/" => BinaryOp.FDiv
        };
    }
}