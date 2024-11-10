using System.Reflection;
using System.Runtime.Versioning;
using DistIL.AsmIO;

namespace TestCompiler;

public class Driver
{
    public bool DebugSymbols = false;
    public bool IsDebug = false;
    public ModuleResolver ModuleResolver = new();
    public bool Optimize = false;
    public string[] Sources;
    public string OutputPath { get; set; }
    public string RootNamespace { get; set; }
    public Version Version { get; set; } = new(1, 0);

    public void Compile()
    {
        ModuleResolver.AddTrustedSearchPaths();
        ModuleResolver.Import(typeof(TargetFrameworkAttribute));
        ModuleResolver.Import(typeof(Console));

        var module = ModuleResolver.Create(RootNamespace, Version);
        var ctor = ModuleResolver.FindMethod("System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(this, string)");

        var customAttrib = new CustomAttrib(ctor, [".NETCoreApp,Version=v8.0"], []);
        module.GetCustomAttribs(true).Add(customAttrib);

        DisplayClassGenerator.Generate(module, out var displayField);

        var program = module.CreateType(RootNamespace, "Program",
            TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.BeforeFieldInit);
        var main = program.CreateMethod("Main", new TypeSig(PrimType.Void), [],
            MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig);

        var src = string.Join("\n", Sources).Replace("\r", "");
        var parser = new ExpressionGrammar();
        var tree = parser.Parse(src);

        var emitter = new Emitter();
        emitter.Emit(tree.Tree, main, this);

        module.EntryPoint = main;

        module.Save(OutputPath, DebugSymbols);
    }
}