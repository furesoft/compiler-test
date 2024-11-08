using DistIL.Attributes;

namespace TestDll;

[Optimize]
public class Loops
{
    [Optimize]
    public static int Count()
    {
        var x = 0;
        for (int i = 0; i < 9; i++)
        {
            x++;
        }

        return x;
    }
}