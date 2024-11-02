
using System.Collections.Immutable;
using System.Reflection;
using DistIL;
using DistIL.Analysis;
using DistIL.AsmIO;
using DistIL.CodeGen.Cil;
using DistIL.IR;
using MethodBody = DistIL.IR.MethodBody;
using Version = System.Version;

public class Program
{
    public static void Main()
    {
        var moduleResolver = new ModuleResolver();
        moduleResolver.AddTrustedSearchPaths();

        var module = moduleResolver.Create("compiled", new Version(1, 0, 1));
        var targetFrameworkType = moduleResolver.Import(typeof(System.Runtime.Versioning.TargetFrameworkAttribute));
        var ctor = targetFrameworkType.FindMethod(".ctor",
            new MethodSig(moduleResolver.SysTypes.Void, [new TypeSig(moduleResolver.SysTypes.String)]));

        var customAttrib = new CustomAttrib(ctor, [".NETCoreApp,Version=v8.0"], []);

        module.GetCustomAttribs(true).Add(customAttrib);

        var program = module.CreateType("compiled", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.BeforeFieldInit);

        var main = program.CreateMethod("Main", new TypeSig(moduleResolver.SysTypes.Void), [], MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig);
        var trueConst = ConstInt.Create(moduleResolver.SysTypes.Boolean, 1);

        var builder = new MethodBody(main);
        var gh = builder.CreateVar(moduleResolver.Import(typeof(int)), "ggh");

        var bb = builder.CreateBlock();

        var consoleType = moduleResolver.Import(typeof(Console));
        var writeLine = consoleType.FindMethod("WriteLine",
            new MethodSig(moduleResolver.SysTypes.Void, [new TypeSig(moduleResolver.SysTypes.String)]));

       // bb.InsertLast(new StoreInst(gh, trueConst));

        bb.InsertLast(new CallInst(writeLine, [ConstString.Create("Hello World!")]));
        bb.InsertLast(new ReturnInst());

        bb.SetName("b");
        main.Body = builder;
        main.ILBody = ILGenerator.GenerateCode(main.Body);
        module.EntryPoint = main;

        module.Save("compiled.dll", false);
    }
}