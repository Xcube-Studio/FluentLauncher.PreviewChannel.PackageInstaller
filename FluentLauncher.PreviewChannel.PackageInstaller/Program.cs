using FluentLauncher.PreviewChannel.PackageInstaller.Scripts;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Compression;
using System.Runtime.InteropServices;

var manualTargetPackageCommand = new Command("manualTargetPackage");
manualTargetPackageCommand.AddOption(CertificationPath);
manualTargetPackageCommand.AddOption(PackagePath);
manualTargetPackageCommand.AddOption(DependencyPackagesPath);
manualTargetPackageCommand.AddOption(LaunchAfterInstalled);

var queryCommand = new Command("query");
queryCommand.AddOption(GetBuildCountOfVersion);
queryCommand.SetHandler(async (versionToGetBuildCount) => await QueryScripts.QueryAsync(versionToGetBuildCount), GetBuildCountOfVersion);

var generateReleaseJsonCommand = new Command("generateReleaseJson");
generateReleaseJsonCommand.AddOption(UpdatePackageFiles);
generateReleaseJsonCommand.AddOption(StableVersion);
generateReleaseJsonCommand.AddOption(Commit);
generateReleaseJsonCommand.SetHandler(async (updatePackageFiles, stableVersion, commit)
    => await ReleaseScripts.GenerateReleaseJson(commit, stableVersion, updatePackageFiles), UpdatePackageFiles, StableVersion, Commit);

manualTargetPackageCommand.SetHandler(async (certificationPath, packagePath, dependencyPackagesPath, launchAfterInstalled) 
    => await InstallScripts.InstallPackage(packagePath, dependencyPackagesPath, certificationPath, launchAfterInstalled),
    CertificationPath, PackagePath, DependencyPackagesPath, LaunchAfterInstalled);

var rootCommand = new RootCommand();
rootCommand.AddOption(LaunchAfterInstalled);
rootCommand.SetHandler(async (launchAfterInstalled) =>
{
    string architecture = RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X64 => "x64",
        Architecture.Arm64 => "arm64",
        _ => throw new NotSupportedException("not supported architecture")
    };

    // Extract Update Package
    var updatePackageFile = new FileInfo($"updatePackage-{architecture}.zip");
    if (!updatePackageFile.Exists)
        throw new FileNotFoundException($"\"updatePackage-{architecture}.zip\" file not found", $"updatePackage-{architecture}.zip");

    ZipFile.ExtractToDirectory(updatePackageFile.FullName, $"updatePackage-{architecture}", true);

    await InstallScripts.InstallPackage(
        $"updatePackage-{architecture}\\msix-{architecture}.msix", 
        Directory.GetFiles($"updatePackage-{architecture}\\dependencies"),
        launchAfterInstalled: launchAfterInstalled);

    // Clean up
    Directory.Delete($"updatePackage-{architecture}", true);
}, LaunchAfterInstalled);

rootCommand.AddCommand(manualTargetPackageCommand);
rootCommand.AddCommand(queryCommand);
rootCommand.AddCommand(generateReleaseJsonCommand);

return await rootCommand.InvokeAsync(args);

public partial class Program
{
    static Option<string?> CertificationPath { get; } = new(name: "--certificationPath");

    static Option<string> PackagePath { get; } = new(name: "--packagePath") { IsRequired = true };

    static Option<string[]> DependencyPackagesPath { get; } = new(name: "--dependencyPackagesPath") 
    {
        IsRequired = true, 
        AllowMultipleArgumentsPerToken = true 
    };

    static Option<bool> LaunchAfterInstalled { get; } = new(name: "--launchAfterInstalled", getDefaultValue: () => true) { IsRequired = false,  };

    static Option<string> GetBuildCountOfVersion { get; } = new(name: "--getBuildCountOfVersion") { IsRequired = false };

    static Option<string> StableVersion { get; } = new(name: "--stableVersion") { IsRequired = true, };

    static Option<string> Commit { get; } = new(name: "--commit") { IsRequired = true, };

    static Option<string[]> UpdatePackageFiles { get; } = new(name: "--updatePackageFiles") 
    { 
        IsRequired = true,
        AllowMultipleArgumentsPerToken = true
    };
}