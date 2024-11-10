using DistIL;
using DistIL.IR.Utils;
using DistIL.Passes;

namespace TestCompiler.Passes;

internal class VerificationPass : IMethodPass
{
    public MethodPassResult Run(MethodTransformContext ctx)
    {
        var diags = IRVerifier.Diagnose(ctx.Method);

        if (diags.Count > 0)
        {
            using var scope =
                ctx.Logger.Push(new LoggerScopeInfo("DistIL.IR.Verification"), $"Bad IR in '{ctx.Method}'");

            foreach (var diag in diags) ctx.Logger.Warn(diag.ToString());
        }

        return MethodInvalidations.None;
    }
}