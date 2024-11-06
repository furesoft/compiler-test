using System.Reflection;
using DistIL;
using DistIL.AsmIO;
using DistIL.CodeGen.Cil;
using DistIL.IR;
using DistIL.IR.Utils;
using DistIL.Passes;
using Silverfly.Nodes;
using Silverfly.Nodes.Operators;
using TestCompiler.Nodes;
using TestCompiler.Passes;
using MethodBody = DistIL.IR.MethodBody;

namespace TestCompiler;

public class Emitter
{

    public void Emit(AstNode node, MethodDef method)
    {
        method.Body = new MethodBody(method);

        var entryBlock = method.Body.CreateBlock();
        var builder = new DistIL.IR.Utils.IRBuilder(entryBlock);

        Value? result = null;
        for (var index = 0; index < ((BlockNode)node).Children.Count; index++)
        {
            var child = ((BlockNode)node).Children[index];
            _ = Emit(child, builder);
        }

        Optimize(method);
        //method.ILBody = ILGenerator.GenerateCode(method.Body);
    }

    private static void Optimize(MethodDef main)
    {
        var passManager = new PassManager
        {
            Compilation = new Compilation(main.DeclaringType.Module, new ConsoleLogger(), new CompilationSettings()),
        };

        var passes = passManager.AddPasses();
        passes.Apply<InlineMethods>();
        passes.Apply<SimplifyInsts>();
        passes.Apply<SimplifyCFG>();
        passes.Apply<ValueNumbering>();
        passes.Apply<DeadCodeElim>();
        passes.Apply<InsertReturnPass>();
        passes.Apply<VerificationPass>();
        passes.Apply(new DumpPass(_ => true));

        passes.Run(new MethodTransformContext(passManager.Compilation, main.Body), []);
    }

    private Value? Emit(AstNode node, IRBuilder builder)
    {
        return node switch
        {
            LiteralNode literal => EmitLiteral(literal),
            BinaryOperatorNode bin => EmitBinary(bin, builder),
            CallNode call => EmitCall(call, builder),
            GroupNode group when group.LeftSymbol == "(" => EmitGroup(group, builder),
            VariableBindingNode let => EmitVariableBinding(let, builder),
            LambdaNode lambda => EmitLambda(lambda, builder),
            NameNode name => EmitName(name, builder),
            _ => null
        };
    }

    private Value? EmitLambda(LambdaNode lambda, IRBuilder builder)
    {
        var method = DisplayClassGenerator.GenerateLambda(lambda, builder);

        return new PhiInst(PrimType.Void);
    }

    private Value? EmitName(NameNode name, IRBuilder builder)
    {
        if (_variables.TryGetValue(name.Token.ToString(), out var variable))
        {
            return builder.CreateLoad(variable);
        }

        var arg = builder.Method.Args.FirstOrDefault(_ => _.Name == name.Token.ToString());
        if (arg != null)
        {
            return arg;
        }

        return null;
    }

    Dictionary<string, LocalSlot> _variables = new();
    private Value? EmitVariableBinding(VariableBindingNode let, IRBuilder builder)
    {
        if (let.Parameters.Any())
        {
            return DefineFunction(let, builder);
        }

        var result = Emit(let.Value, builder);
        var variable = builder.Method.Definition.Body.CreateVar(result.ResultType, let.Name.ToString());

        _variables.Add(let.Name.ToString(), variable);

        return builder.CreateStore(variable, result);
    }

    private Value? DefineFunction(VariableBindingNode let, IRBuilder builder)
    {
        var parameters = let.Parameters.Select(_ => new ParamDef(PrimType.Int64, _.Token.ToString()));
        var method = builder.Method.Definition.DeclaringType.CreateMethod(let.Name.ToString(), PrimType.Int64, [..parameters], MethodAttributes.Public | MethodAttributes.Static);

        var body = let.Value;
        if (let.Value is not BlockNode)
        {
            body = new BlockNode("", "").WithChildren([let.Value]);
        }

        var emitter = new Emitter();
        emitter.Emit(body, method);

        return new PhiInst(PrimType.Int64);
    }

    private Value? EmitGroup(GroupNode group, IRBuilder builder)
    {
        return Emit(group.Expr, builder);
    }

    private Value EmitLiteral(LiteralNode node)
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

    private Value EmitCall(CallNode call, IRBuilder builder)
    {
        var moduleResolver = builder.Method.Definition.DeclaringType.Module.Resolver;

        var consoleType = moduleResolver.Import(typeof(Console));

        if (call.FunctionExpr is NameNode n)
        {
            if (n.Token.ToString() is "print")
            {
                var value = Emit(call.Arguments[0], builder);

                var writeLine = moduleResolver.FindMethod("System.Console::WriteLine", [value]);

                return builder.CreateCall(writeLine, value);
            }
            else if (n.Token.ToString() is "sizeOf")
            {
                var type = PrimType.Int32; //ToDo: add type resolving from arg

                builder.Emit(new CilIntrinsic.SizeOf(type));
            }
        }
    

        return null;
    }


    private Value EmitBinary(BinaryOperatorNode node, IRBuilder builder)
    {
        var op = MapBinOperator(node.Operator.Text.ToString());

        var left = Emit(node.LeftExpr, builder);
        var right = Emit(node.RightExpr, builder);

        return builder.CreateBin(op, left, right);
    }

    private BinaryOp MapBinOperator(string op) =>
        op switch
        {
            "+" => BinaryOp.Add,
            "-" => BinaryOp.Sub,
            "*" => BinaryOp.Mul,
            "/" => BinaryOp.FDiv,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
}