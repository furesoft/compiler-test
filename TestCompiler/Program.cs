using System.Reflection;
using DistIL;
using DistIL.AsmIO;
using DistIL.IR;
using DistIL.Passes;
using TestCompiler.Passes;
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

        var module = moduleResolver.Create("compiled", new Version(1, 0, 1));
        var targetFrameworkType = moduleResolver.Import(typeof(System.Runtime.Versioning.TargetFrameworkAttribute));
        var ctor = targetFrameworkType.FindMethod(".ctor",
            new MethodSig(moduleResolver.SysTypes.Void, [new TypeSig(moduleResolver.SysTypes.String)]));

        var customAttrib = new CustomAttrib(ctor, [".NETCoreApp,Version=v8.0"], []);
        module.GetCustomAttribs(true).Add(customAttrib);

        DisplayClassGenerator.Generate(module, out displayField);

        var program = module.CreateType("compiled", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.BeforeFieldInit);
        var main = program.CreateMethod("Main", new TypeSig(PrimType.Void), [], MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig);var trueConst = ConstInt.Create(moduleResolver.SysTypes.Boolean, 1);

        var emitter = new Emitter();
        emitter.Emit(tree.Tree, main);

        module.EntryPoint = main;

        module.Save("compiled.dll", false);
    }
}