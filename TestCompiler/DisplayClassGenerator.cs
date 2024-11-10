using System.Reflection;
using DistIL.AsmIO;
using DistIL.IR.Utils;
using Silverfly.Nodes;
using TestCompiler.Nodes;

namespace TestCompiler;

public static class DisplayClassGenerator
{
    public static TypeDesc Generate(ModuleDef module, out FieldDef fieldDef)
    {
        var type = module.CreateType("compiled", "<>Program", TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.BeforeFieldInit);

        fieldDef = type.CreateField("<>Program1", new TypeSig(type), FieldAttributes.InitOnly | FieldAttributes.Static);

        return type;
    }

    public static MethodDef GenerateLambda(LambdaNode lambda, IRBuilder builder, Driver driver)
    {
        var parameters = lambda.Parameters.Select(_ => new ParamDef(PrimType.Int64, _.Token.ToString()));
        var method = builder.Method.Definition.DeclaringType.CreateMethod("<Program>b__0_0", PrimType.Int64, [..parameters], MethodAttributes.Public | MethodAttributes.Static);

        var body = lambda.Value;
        if (lambda.Value is not BlockNode)
        {
            body = new BlockNode("", "").WithChildren([lambda.Value]);
        }

        var emitter = new Emitter();
        emitter.Emit(body, method, driver);

        return method;
    }
}