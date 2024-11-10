using DistIL.AsmIO;
using DistIL.IR;
using DistIL.Passes;

namespace TestCompiler;

public class InsertReturnPass : IMethodPass
{
    public MethodPassResult Run(MethodTransformContext ctx)
    {
        if (ctx.Definition.ReturnSig.Type != PrimType.Void)
            return new MethodPassResult { Changes = MethodInvalidations.None };

        foreach (var block in ctx.Definition.Body)
            if (block.Last.IsBranch)
                return new MethodPassResult { Changes = MethodInvalidations.None };

        var lastBlock = GetLastBlock(ctx.Definition.Body.EntryBlock);
        lastBlock.InsertLast(new ReturnInst());

        return new MethodPassResult { Changes = MethodInvalidations.ControlFlow };
    }

    private BasicBlock GetLastBlock(BasicBlock entryBlock)
    {
        var lastBlock = entryBlock;

        while (lastBlock.Next != null) lastBlock = lastBlock.Next;

        return lastBlock;
    }
}