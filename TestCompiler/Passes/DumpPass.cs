namespace TestCompiler.Passes;

class DumpPass(Predicate<MethodDef?> filter) : IMethodPass
{
    public Predicate<MethodDef>? Filter { get; init; } = filter;

    public MethodPassResult Run(MethodTransformContext ctx)
    {
        if (Filter == null || Filter.Invoke(ctx.Method.Definition)) {
            IRPrinter.ExportPlain(ctx.Method, Console.Out);
        }

        return MethodInvalidations.None;
    }
}