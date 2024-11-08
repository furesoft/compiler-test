using DistIL;
using DistIL.CodeGen.Cil;
using DistIL.IR.Utils;
using DistIL.Passes;
using TestCompiler.Passes;
using TestDll;

namespace TestCompiler;

public class Program
{
    public static void Main()
    {
        Driver.Sources = ["let x = 4+2\nprint(x)"];

        Driver.Compile();

    /*    var loop = new LoopBuilder(main.Body.CreateBlock());
        var accu = main.Body.CreateVar(PrimType.Int32, "accu");
        var index = main.Body.CreateVar(PrimType.Int32, "index");
        loop.Build((ir) => ir.CreateCmp(CompareOp.Slt, ir.CreateLoad(index), ConstInt.CreateI(9)), builder =>
        {
            // accu += 1
            var left = builder.CreateLoad(accu);
            var right = ConstInt.CreateI(1);
            var increment = builder.CreateBin(BinaryOp.Add, left, right);
            builder.CreateStore(accu, increment);
        });

        loop.InsertBefore(main.Body.EntryBlock.Last);*/

        //main.ILBody = ILGenerator.GenerateCode(main.Body);
    }
}