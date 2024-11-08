using System.Reflection;
using DistIL.AsmIO;
using DistIL.CodeGen.Cil;
using DistIL.IR;

namespace TestCompiler;

public static class Driver
{
    public static ModuleResolver moduleResolver = new();
    public static string[] Sources;
    public static bool IsDebug = false;
    public static string OutputPath;

    public static void Compile()
    {
        moduleResolver.AddTrustedSearchPaths();
        moduleResolver.Import(typeof(System.Runtime.Versioning.TargetFrameworkAttribute));
        moduleResolver.Import(typeof(System.Console));

        var module = moduleResolver.Create("compiled", new Version(1, 0, 1));
        var ctor = moduleResolver.FindMethod("System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(this, string)");

        var customAttrib = new CustomAttrib(ctor, [".NETCoreApp,Version=v8.0"], []);
        module.GetCustomAttribs(true).Add(customAttrib);

        DisplayClassGenerator.Generate(module, out var displayField);

        var program = module.CreateType("compiled", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.BeforeFieldInit);
        var main = program.CreateMethod("Main", new TypeSig(PrimType.Void), [], MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig);

        var src = string.Join("\n", Sources).Replace("\r", "");
        var parser = new ExpressionGrammar();
        var tree = parser.Parse(src);

        var emitter = new Emitter();
        emitter.Emit(tree.Tree, main);

        main.ILBody = ILGenerator.GenerateCode(main.Body);
        module.EntryPoint = main;

        module.Save(Path.Combine(OutputPath, "compiled.dll"), false);
    }
}