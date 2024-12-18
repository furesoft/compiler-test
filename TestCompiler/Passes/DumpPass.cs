using DistIL.AsmIO;
using DistIL.IR.Utils;
using DistIL.Passes;

namespace TestCompiler.Passes;

internal class DumpPass(Predicate<MethodDef?> filter) : IMethodPass
{
    public Predicate<MethodDef>? Filter { get; init; } = filter;

    public MethodPassResult Run(MethodTransformContext ctx)
    {
        if (Filter == null || Filter.Invoke(ctx.Method.Definition)) IRPrinter.ExportPlain(ctx.Method, Console.Out);

        return MethodInvalidations.None;
    }
}