using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using DistIL.AsmIO;
using DistIL.IR.Utils;
using Silverfly;
using Silverfly.Text;
using SourceDocument = DistIL.AsmIO.SourceDocument;

namespace TestCompiler;

public class Driver
{
    public bool DebugSymbols = false;
    public bool IsDebug = false;
    public ModuleResolver ModuleResolver = new();
    public bool Optimize = false;
    public string[] Sources;
    public string OutputPath { get; set; } = "compiled.dll";
    public string RootNamespace { get; set; } = "";
    public Version Version { get; set; } = new(1, 0);

    public Silverfly.Text.SourceDocument[] Compile()
    {
        ModuleResolver.AddTrustedSearchPaths();
        ModuleResolver.Import(typeof(TargetFrameworkAttribute));
        ModuleResolver.Import(typeof(Console));
        ModuleResolver.Import(typeof(GuidAttribute));

        var module = ModuleResolver.Create(RootNamespace, Version);
        var targetFrameworkCtor = ModuleResolver.FindMethod("System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(this, string)");
        var guidAttrCtor = ModuleResolver.FindMethod("System.Runtime.InteropServices.GuidAttribute::.ctor(this, string)");

        var targetFrameworkAttr = new CustomAttrib(targetFrameworkCtor, [".NETCoreApp,Version=v8.0"], []);
        module.GetCustomAttribs(true).Add(targetFrameworkAttr);

        var guidAttr = new CustomAttrib(guidAttrCtor, [Guid.NewGuid().ToString()], []);
        module.GetCustomAttribs(true).Add(guidAttr);

        DisplayClassGenerator.Generate(module, out var displayField);

        var program = module.CreateType(RootNamespace, "Program",
            TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.BeforeFieldInit);
        var main = program.CreateMethod("Main", new TypeSig(PrimType.Void), [],
            MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig);

        var parser = new ExpressionGrammar();

        List<TranslationUnit> translationUnits = [];
        foreach (var source in Sources)
        {
            var content = File.ReadAllText(source).Replace("\r", "");

            var tree = parser.Parse(content, source);

            translationUnits.Add(tree);

            if (tree.Document.HasErrors)
            {
                goto result;
            }

            var emitter = new Emitter();
            emitter.Emit(tree.Tree, main, this);
        }

        module.EntryPoint = main;
        module.Save(OutputPath, DebugSymbols);

        result:
        return translationUnits.Select(tu => tu.Document).ToArray();
    }
}