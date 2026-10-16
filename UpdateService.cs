using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JieipManager;

public sealed record ReleaseInfo(Version Version, string DownloadUrl, string Notes);

public static class UpdateService
{
    private const string LatestReleaseApi = "https://api.github.com/repos/boramee/jieip-manager/releases/latest";
    private const string AssetName = "JieipManager-win-x64.exe";

    public static Version CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

    public static async Task<ReleaseInfo> GetLatestReleaseAsync()
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"JieipManager/{CurrentVersion}");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        using var response = await http.GetAsync(LatestReleaseApi);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var release = JsonSerializer.Deserialize<GitHubRelease>(json) ?? throw new InvalidOperationException("GitHub Release 정보를 읽을 수 없습니다.");
        var versionText = release.TagName.Trim().TrimStart('v', 'V');
        if (!Version.TryParse(versionText, out var version)) throw new InvalidOperationException($"알 수 없는 버전 형식입니다: {release.TagName}");
        var asset = release.Assets.FirstOrDefault(x => string.Equals(x.Name, AssetName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"업데이트 파일({AssetName})이 Release에 없습니다.");
        return new ReleaseInfo(version, asset.DownloadUrl, release.Body ?? "");
    }

    public static bool IsNewer(Version latest) => latest > CurrentVersion;

    public static async Task DownloadAndRestartAsync(ReleaseInfo release)
    {
        var currentExe = Environment.ProcessPath ?? throw new InvalidOperationException("현재 실행 파일 경로를 찾을 수 없습니다.");
        var tempExe = Path.Combine(Path.GetTempPath(), $"JieipManager-{release.Version}-{Guid.NewGuid():N}.exe");
        using (var http = new HttpClient())
        {
            http.DefaultRequestHeaders.UserAgent.ParseAdd($"JieipManager/{CurrentVersion}");
            using var response = await http.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using var source = await response.Content.ReadAsStreamAsync();
            await using var target = File.Create(tempExe);
            await source.CopyToAsync(target);
        }

        if (new FileInfo(tempExe).Length < 1_000_000)
            throw new InvalidOperationException("받은 업데이트 파일이 비정상적으로 작습니다.");

        var scriptPath = Path.Combine(Path.GetTempPath(), $"JieipManager-update-{Guid.NewGuid():N}.ps1");
        var script = $$"""
            $ErrorActionPreference = 'Stop'
            $pidToWait = {{Environment.ProcessId}}
            $source = '{{PsQuote(tempExe)}}'
            $target = '{{PsQuote(currentExe)}}'
            Wait-Process -Id $pidToWait -ErrorAction SilentlyContinue
            for ($i = 0; $i -lt 20; $i++) {
                try {
                    Copy-Item -LiteralPath $source -Destination $target -Force
                    Start-Process -FilePath $target
                    Remove-Item -LiteralPath $source -Force -ErrorAction SilentlyContinue
                    Remove-Item -LiteralPath $PSCommandPath -Force -ErrorAction SilentlyContinue
                    exit 0
                } catch {
                    Start-Sleep -Milliseconds 500
                }
            }
            Add-Type -AssemblyName PresentationFramework
            [System.Windows.MessageBox]::Show('업데이트 파일 교체에 실패했습니다. 프로그램 폴더의 쓰기 권한을 확인하세요.', '지입관리 업데이트') | Out-Null
            """;
        await File.WriteAllTextAsync(scriptPath, script, new System.Text.UTF8Encoding(true));
        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }

    private static string PsQuote(string value) => value.Replace("'", "''");

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string TagName { get; set; } = "";
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("assets")] public List<GitHubAsset> Assets { get; set; } = [];
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("browser_download_url")] public string DownloadUrl { get; set; } = "";
    }
}
