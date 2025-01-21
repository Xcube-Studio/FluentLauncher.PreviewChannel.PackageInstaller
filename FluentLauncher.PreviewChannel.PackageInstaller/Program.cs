using FluentLauncher.PreviewChannel.PackageInstaller.Scripts;
using System.CommandLine;
using System.IO.Compression;
using System.Runtime.InteropServices;

Arguments = args;

var manualTargetPackageCommand = new Command("manualTargetPackage");
manualTargetPackageCommand.AddOption(CertificationPath);
manualTargetPackageCommand.AddOption(PackagePath);
manualTargetPackageCommand.AddOption(DependencyPackagesPath);
manualTargetPackageCommand.AddOption(LaunchAfterInstalled);
manualTargetPackageCommand.AddOption(LogFilePath);
manualTargetPackageCommand.SetHandler(async (certificationPath, packagePath, dependencyPackagesPath, launchAfterInstalled, logFilePath)
    => await InstallScripts.InstallPackage(packagePath, dependencyPackagesPath, certificationPath, launchAfterInstalled, logFilePath),
    CertificationPath, PackagePath, DependencyPackagesPath, LaunchAfterInstalled, LogFilePath);

var queryCommand = new Command("query");
queryCommand.AddOption(GetBuildCountOfVersion);
queryCommand.SetHandler(async (versionToGetBuildCount) => await QueryScripts.QueryAsync(versionToGetBuildCount), GetBuildCountOfVersion);

var generateReleaseJsonCommand = new Command("generateReleaseJson");
generateReleaseJsonCommand.AddOption(UpdatePackageFiles);
generateReleaseJsonCommand.AddOption(StableVersion);
generateReleaseJsonCommand.AddOption(Commit);
generateReleaseJsonCommand.SetHandler(async (updatePackageFiles, stableVersion, commit)
    => await ReleaseScripts.GenerateReleaseJson(commit, stableVersion, updatePackageFiles), UpdatePackageFiles, StableVersion, Commit);

var generateReleaseMarkdownCommand = new Command("generateReleaseMarkdown");
generateReleaseMarkdownCommand.AddOption(UpdatePackageFiles);
generateReleaseMarkdownCommand.AddOption(StableVersion);
generateReleaseMarkdownCommand.AddOption(Commit);
generateReleaseMarkdownCommand.AddOption(MarkdownPath);
generateReleaseMarkdownCommand.SetHandler(async (updatePackageFiles, stableVersion, commit, markdownPath)
    => await ReleaseScripts.GenerateReleaseMarkdown(markdownPath, commit, stableVersion, updatePackageFiles), UpdatePackageFiles, StableVersion, Commit, MarkdownPath);

var rootCommand = new RootCommand();
rootCommand.AddOption(LaunchAfterInstalled);
rootCommand.AddOption(LogFilePath);
rootCommand.SetHandler(async (launchAfterInstalled, logFilePath) =>
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
        launchAfterInstalled: launchAfterInstalled,
        logFilePath: logFilePath);

    // Clean up
    Directory.Delete($"updatePackage-{architecture}", true);
}, LaunchAfterInstalled, LogFilePath);

rootCommand.AddCommand(manualTargetPackageCommand);
rootCommand.AddCommand(queryCommand);
rootCommand.AddCommand(generateReleaseJsonCommand);
rootCommand.AddCommand(generateReleaseMarkdownCommand);

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

    static Option<string?> LogFilePath { get; } = new(name: "--logFilePath") { IsRequired = false };

    static Option<string> GetBuildCountOfVersion { get; } = new(name: "--getBuildCountOfVersion") { IsRequired = false };

    static Option<string> StableVersion { get; } = new(name: "--stableVersion") { IsRequired = true, };

    static Option<string> Commit { get; } = new(name: "--commit") { IsRequired = true, };

    static Option<string> MarkdownPath { get; } = new(name: "--markdownPath") { IsRequired = true, };

    static Option<string[]> UpdatePackageFiles { get; } = new(name: "--updatePackageFiles") 
    { 
        IsRequired = true,
        AllowMultipleArgumentsPerToken = true
    };

    public static string[] Arguments { get; set; } = null!;
}