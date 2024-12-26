using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace FluentLauncher.PreviewChannel.PackageInstaller.Scripts;

//{
//    "commit": "7c9061e",
//    "build": 1,
//    "releaseTime": "",
//    "previousStableVersion": "2.3.2.0",
//    "currentPreviewVersion": "2.3.2.1",
//    "hashes": {
//        "updatePackage-x64.zip": "",
//        "updatePackage-arm64.zip": ""
//    }
//}

public static partial class ReleaseScripts
{
    public static async Task GenerateReleaseJson(string commit, string stableVersion, string[] packageFiles)
    {
        int build = await QueryScripts.GetBuildCountOfVersionAsync(stableVersion);
        string currentPreviewVersion = VersionRegex().Replace(stableVersion, match =>
        {
            string prefix = match.Groups[1].Value;
            return prefix + "." + build;
        });

        JsonObject json = new()
        {
            { "commit", commit },
            { "build", build + 1 },
            { "releaseTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "currentPreviewVersion", currentPreviewVersion },
            { "previousStableVersion", stableVersion }
        };

        JsonObject hashes = [];

        foreach (var packageFile in packageFiles)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(packageFile);
            byte[] hashBytes = md5.ComputeHash(stream);

            string hash = Convert.ToHexStringLower(hashBytes);
            hashes.Add(Path.GetFileName(packageFile), hash);
        }

        json.Add("hashes", hashes);
        Console.WriteLine(json.ToString());
    }

    [GeneratedRegex(@"(\d+\.\d+\.\d+)\.(\d+)")]
    private static partial Regex VersionRegex();
}
