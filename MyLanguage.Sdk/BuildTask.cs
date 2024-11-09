using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using TestCompiler;

namespace MyLanguage.Build.Tasks;

public class BuildTask : Microsoft.Build.Utilities.Task
{
    [System.ComponentModel.DataAnnotations.Required]
    public ITaskItem[] SourceFiles { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public string OutputPath { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public ITaskItem[] ReferencePaths { get; set; }

    public override bool Execute()
    {
        var fi = new FileInfo(OutputPath);
        var dir = fi.Directory.ToString();

        Driver.moduleResolver.AddTrustedSearchPaths();

        Driver.OutputPath = OutputPath;
        Driver.Sources = SourceFiles.Select(_ => File.ReadAllText(_.ItemSpec)).ToArray();

        foreach (var reference in ReferencePaths)
        {
            try
            {
               // Driver.moduleResolver.Load(reference.ItemSpec);
            }
            catch
            {

            }
        }

        Driver.Compile();
       // File.Copy(OutputPath, Path.Combine(dir, "refint", fi.Name), true);

        return true;
    }
}