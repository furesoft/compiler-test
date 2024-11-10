﻿using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using TestCompiler;

namespace MyLanguage.Build.Tasks;

public class BuildTask : Task
{
    [System.ComponentModel.DataAnnotations.Required]
    public ITaskItem[] SourceFiles { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public string OutputPath { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public ITaskItem[] ReferencePaths { get; set; }

    public bool Optimize { get; set; }
    public bool DebugSymbols { get; set; }
    public string Configuration { get; set; }
    public string Version { get; set; }
    public string RootNamespace { get; set; }

    public override bool Execute()
    {
        var driver = new Driver();

        driver.ModuleResolver.AddTrustedSearchPaths();

        driver.OutputPath = OutputPath;
        driver.RootNamespace = RootNamespace;
        driver.Sources = SourceFiles.Select(_ => File.ReadAllText(_.ItemSpec)).ToArray();
        driver.Optimize = Optimize;
        driver.DebugSymbols = DebugSymbols;
        driver.IsDebug = Configuration == "Debug";
        driver.Version = System.Version.Parse(Version);

        foreach (var reference in ReferencePaths)
            try
            {
                // Driver.moduleResolver.Load(reference.ItemSpec);
            }
            catch
            {
            }

        driver.Compile();
        // File.Copy(OutputPath, Path.Combine(dir, "refint", fi.Name), true);

        return true;
    }
}