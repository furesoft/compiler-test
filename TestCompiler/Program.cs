using System.Reflection;
using DistIL;
using DistIL.AsmIO;
using DistIL.CodeGen.Cil;
using DistIL.IR;
using DistIL.IR.Utils;
using DistIL.Passes;
using TestCompiler.Passes;
using TestDll;
using Version = System.Version;

namespace TestCompiler;

public class Program
{
    public static FieldDef displayField;
    public static void Main()
    {
        var parser = new ExpressionGrammar();
        var tree = parser.Parse("let x = 4+2\nprint(x)");

        var moduleResolver = new ModuleResolver();
        moduleResolver.AddTrustedSearchPaths();
        moduleResolver.Import(typeof(System.Runtime.Versioning.TargetFrameworkAttribute));
        moduleResolver.Import(typeof(System.Console));

        var module = moduleResolver.Create("compiled", new Version(1, 0, 1));
        var ctor = moduleResolver.FindMethod("System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(this, string)");

        var customAttrib = new CustomAttrib(ctor, [".NETCoreApp,Version=v8.0"], []);
        module.GetCustomAttribs(true).Add(customAttrib);

        DisplayClassGenerator.Generate(module, out displayField);

        var program = module.CreateType("compiled", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.BeforeFieldInit);
        var main = program.CreateMethod("Main", new TypeSig(PrimType.Void), [], MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig);var trueConst = ConstInt.Create(moduleResolver.SysTypes.Boolean, 1);

        var emitter = new Emitter();

        emitter.Emit(tree.Tree, main);
        var loop = new LoopBuilder(main.Body.CreateBlock());
        var accu = main.Body.CreateVar(PrimType.Int32, "accu");
        var index = main.Body.CreateVar(PrimType.Int32, "index");
        loop.Build((ir) => ir.CreateCmp(CompareOp.Slt, ir.CreateLoad(index), ConstInt.CreateI(9)), builder =>
        {
            // accu += 1
            var left = builder.CreateLoad(accu);
            var right = ConstInt.CreateI(1);
            var increment = builder.CreateBin(BinaryOp.Add, left, right);
            builder.CreateStore(accu, increment);
        });

        loop.InsertBefore(main.Body.EntryBlock.Last);

        //main.ILBody = ILGenerator.GenerateCode(main.Body);

        module.EntryPoint = main;

        module.Save("compiled.dll", false);
    }
}