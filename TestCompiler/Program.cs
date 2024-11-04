using System.Reflection;
using DistIL;
using DistIL.AsmIO;
using DistIL.CodeGen.Cil;
using DistIL.IR;
using DistIL.Passes;
using Version = System.Version;

namespace TestCompiler;

public class Program
{
    public static void Main()
    {
        var parser = new ExpressionGrammar();
        var tree = parser.Parse("let x = 2\nlet add x y = x + y\nprint(x)");

        var moduleResolver = new ModuleResolver();
        moduleResolver.AddTrustedSearchPaths();

        var module = moduleResolver.Create("compiled", new Version(1, 0, 1));
        var targetFrameworkType = moduleResolver.Import(typeof(System.Runtime.Versioning.TargetFrameworkAttribute));
        var ctor = targetFrameworkType.FindMethod(".ctor",
            new MethodSig(moduleResolver.SysTypes.Void, [new TypeSig(moduleResolver.SysTypes.String)]));

        var customAttrib = new CustomAttrib(ctor, [".NETCoreApp,Version=v8.0"], []);
        module.GetCustomAttribs(true).Add(customAttrib);

        var program = module.CreateType("compiled", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.BeforeFieldInit);
        var main = program.CreateMethod("Main", new TypeSig(PrimType.Void), [], MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig);var trueConst = ConstInt.Create(moduleResolver.SysTypes.Boolean, 1);

        var emitter = new Emitter();
        emitter.Emit(tree.Tree, main);

        module.EntryPoint = main;

        module.Save("compiled.dll", false);
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
        passes.Apply<VerificationPass>();
        passes.Apply(new DumpPass(_ => true));

        passes.Run(new MethodTransformContext(passManager.Compilation, main.Body), []);
    }
}