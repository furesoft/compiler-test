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
    private readonly Dictionary<string, LocalSlot> _variables = new();

    public void Emit(AstNode node, MethodDef method, Driver driver)
    {
        method.Body = new MethodBody(method);

        var entryBlock = method.Body.CreateBlock();
        var builder = new IRBuilder(entryBlock);

        foreach (var child in ((BlockNode)node).Children) _ = Emit(child, builder, driver);

        Optimize(method, driver);

        method.ILBody = ILGenerator.GenerateCode(method.Body);
    }

    private static void Optimize(MethodDef main, Driver driver)
    {
        var passManager = new PassManager
        {
            Compilation = new Compilation(main.DeclaringType.Module, new ConsoleLogger(), new CompilationSettings())
        };

        var passes = passManager.AddPasses();

        if (driver.Optimize)
        {
            passes.Apply<InlineMethods>();
            passes.Apply<SimplifyInsts>();
            passes.Apply<SimplifyCFG>();
            passes.Apply<ValueNumbering>();
            passes.Apply<DeadCodeElim>();
        }

        passes.Apply<InsertReturnPass>();
        passes.Apply<VerificationPass>();
        //passes.Apply(new DumpPass(_ => true));

        passes.Run(new MethodTransformContext(passManager.Compilation, main.Body), []);
    }

    private Value? Emit(AstNode node, IRBuilder builder, Driver driver)
    {
        return node switch
        {
            LiteralNode literal => EmitLiteral(literal),
            BinaryOperatorNode bin => EmitBinary(bin, builder, driver),
            CallNode call => EmitCall(call, builder, driver),
            GroupNode group when group.LeftSymbol == "(" => EmitGroup(group, builder, driver),
            VariableBindingNode let => EmitVariableBinding(let, builder, driver),
            LambdaNode lambda => EmitLambda(lambda, builder, driver),
            NameNode name => EmitName(name, builder),
            _ => null
        };
    }

    private Value? EmitLambda(LambdaNode lambda, IRBuilder builder, Driver driver)
    {
        var method = DisplayClassGenerator.GenerateLambda(lambda, builder, driver);

        return new PhiInst(PrimType.Void);
    }

    private Value? EmitName(NameNode name, IRBuilder builder)
    {
        if (_variables.TryGetValue(name.Token.ToString(), out var variable)) return builder.CreateLoad(variable);

        return builder.Method.Args.FirstOrDefault(_ => _.Name == name.Token.ToString());
    }

    private Value? EmitVariableBinding(VariableBindingNode let, IRBuilder builder, Driver driver)
    {
        if (!let.Parameters.IsEmpty) return DefineFunction(let, builder, driver);

        var result = Emit(let.Value, builder, driver);
        var variable = builder.Method.Definition.Body!.CreateVar(result!.ResultType, let.Name.ToString());

        _variables.Add(let.Name.ToString(), variable);

        return builder.CreateStore(variable, result);
    }

    private Value? DefineFunction(VariableBindingNode let, IRBuilder builder, Driver driver)
    {
        var parameters = let.Parameters.Select(_ => new ParamDef(PrimType.Int64, _.Token.ToString()));
        var method = builder.Method.Definition.DeclaringType.CreateMethod(let.Name.ToString(), PrimType.Int64,
            [..parameters], MethodAttributes.Public | MethodAttributes.Static);

        var body = let.Value;
        if (let.Value is not BlockNode) body = new BlockNode("", "").WithChildren([let.Value]);

        var emitter = new Emitter();
        emitter.Emit(body, method, driver);

        return new PhiInst(PrimType.Int64);
    }

    private Value? EmitGroup(GroupNode group, IRBuilder builder, Driver driver)
    {
        return Emit(group.Expr, builder, driver);
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

    private Value EmitCall(CallNode call, IRBuilder builder, Driver driver)
    {
        var moduleResolver = builder.Method.Definition.DeclaringType.Module.Resolver;

        var consoleType = moduleResolver.Import(typeof(Console));

        if (call.FunctionExpr is NameNode n)
        {
            if (n.Token.ToString() is "print")
            {
                var value = Emit(call.Arguments[0], builder, driver);

                var writeLine = moduleResolver.FindMethod("System.Console::WriteLine", [value]);

                return builder.CreateCall(writeLine, value);
            }

            if (n.Token.ToString() is "sizeOf")
            {
                var type = PrimType.Int32; //ToDo: add type resolving from arg

                builder.Emit(new CilIntrinsic.SizeOf(type));
            }
        }


        return null;
    }


    private Value EmitBinary(BinaryOperatorNode node, IRBuilder builder, Driver driver)
    {
        var op = MapBinOperator(node.Operator.Text.ToString());

        var left = Emit(node.LeftExpr, builder, driver);
        var right = Emit(node.RightExpr, builder, driver);

        return builder.CreateBin(op, left, right);
    }

    private BinaryOp MapBinOperator(string op)
    {
        return op switch
        {
            "+" => BinaryOp.Add,
            "-" => BinaryOp.Sub,
            "*" => BinaryOp.Mul,
            "/" => BinaryOp.FDiv,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }
}