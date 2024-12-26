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

public static class QueryScripts
{
    public const string GithubReleasesApi = "https://api.github.com/repos/Xcube-Studio/Natsurainko.FluentLauncher/releases";

    private static HttpClient _httpClient = new();

    static QueryScripts()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0");
    }

    public static async Task QueryAsync(string? versionToGetBuildCount)
    {
        if (!string.IsNullOrEmpty(versionToGetBuildCount))
            Console.WriteLine($"BuildCount: {await GetBuildCountOfVersionAsync(versionToGetBuildCount)}");
    }

    public static async Task<int> GetBuildCountOfVersionAsync(string version)
    {
        string releasesContent = await _httpClient.GetStringAsync(GithubReleasesApi);
        string pattern = @"(?<=``` json)([\s\S]+?)(?=```)";

        foreach (var node in JsonArray.Parse(releasesContent)!.AsArray())
        {
            if (node!.AsObject().ContainsKey("prerelease") && node["prerelease"]!.GetValue<bool>())
            {
                string body = node["body"]!.GetValue<string>();
                Match match = Regex.Match(body, pattern);

                if (!match.Success)
                    continue;

                JsonNode jsonBody = JsonNode.Parse(match.Groups[1].Value)!;

                if (jsonBody["previousStableVersion"]!.GetValue<string>() != version)
                    continue;

                return jsonBody["build"]!.GetValue<int>();
            }
        }

        return 0;
    }
}
