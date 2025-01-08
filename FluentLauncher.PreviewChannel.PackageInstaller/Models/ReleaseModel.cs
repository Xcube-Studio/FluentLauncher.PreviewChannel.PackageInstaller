using System.Text.Json.Serialization;

namespace FluentLauncher.PreviewChannel.PackageInstaller.Models;

public class AssetModel
{
    [JsonPropertyName("name")]
    public required string Name { set; get; }

    [JsonPropertyName("browser_download_url")]
    public required string DownloadUrl { set; get; }
}

public class ReleaseModel
{
    [JsonPropertyName("tag_name")]
    public required string TagName { get; set; }

    [JsonPropertyName("published_at")]
    public required string PublishedAt { set; get; }

    [JsonPropertyName("prerelease")]
    public required bool IsPreRelease { get; set; }

    [JsonPropertyName("body")]
    public required string Body { set; get; }

    [JsonPropertyName("assets")]
    public required AssetModel[] Assets { get; set; }
}
