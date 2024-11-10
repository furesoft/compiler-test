using System.Reflection;
using DistIL.AsmIO;
using DistIL.CodeGen.Cil;
using DistIL.IR;

namespace TestCompiler;

public class Driver
{
    public ModuleResolver ModuleResolver = new();
    public string[] Sources;
    public bool IsDebug = false;
    public bool Optimize = false;
    public bool DebugSymbols = false;
    public string OutputPath { get; set; }
    public Version Version { get; set; } = new(1, 0);

    public void Compile()
    {
        ModuleResolver.AddTrustedSearchPaths();
        ModuleResolver.Import(typeof(System.Runtime.Versioning.TargetFrameworkAttribute));
        ModuleResolver.Import(typeof(Console));

        var module = ModuleResolver.Create("compiled", Version);
        var ctor = ModuleResolver.FindMethod("System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(this, string)");

        var customAttrib = new CustomAttrib(ctor, [".NETCoreApp,Version=v8.0"], []);
        module.GetCustomAttribs(true).Add(customAttrib);

        DisplayClassGenerator.Generate(module, out var displayField);

        var program = module.CreateType("compiled", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.BeforeFieldInit);
        var main = program.CreateMethod("Main", new TypeSig(PrimType.Void), [], MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig);

        var src = string.Join("\n", Sources).Replace("\r", "");
        var parser = new ExpressionGrammar();
        var tree = parser.Parse(src);

        var emitter = new Emitter();
        emitter.Emit(tree.Tree, main, this);

        module.EntryPoint = main;

        module.Save(OutputPath, DebugSymbols);
    }
}