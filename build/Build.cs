using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using Serilog;
using TestCompiler;

class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution]
    Solution Solution { get; set; }

    [Parameter(Name = "tfm")] public string Tfm { get; set; }

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            Log.Debug("TFM: " + Tfm);
            NuGetTasks.NuGetInstall(Configurator);

            NuGetInstallSettings Configurator(NuGetInstallSettings settings)
            {
                return settings.SetPackageID("Newtonsoft.Json").
                SetVersion("13.0.3").
                SetFramework(Tfm).

                SetOutputDirectory(RootDirectory / "tmp");
            }
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            var tmp = RootDirectory / "tmp";
            var dlls = tmp.GlobFiles("**/*.dll");

            Driver.moduleResolver.AddTrustedSearchPaths();
            //Driver.moduleResolver.AddSearchPaths(dlls.Select(x => x.Parent.ToString()));

            foreach (var dll in dlls)
            {
                try
                {
                    Driver.moduleResolver.Load(dll);
                }
                catch (Exception e)
                {
                }
            }

            Driver.IsDebug = Configuration == Configuration.Debug;
            Driver.Sources = RootDirectory.GlobFiles("*.src").Select(_ => _.ReadAllText()).ToArray();
            Driver.OutputPath = RootDirectory;
            Driver.Compile();
        });

}
