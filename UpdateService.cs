using System.Diagnostics;
using System.Reflection;

namespace JieipManager;

public sealed record ReleaseInfo(Version Version, string DownloadUrl, string Notes);

public static class UpdateService
{
    private const string LatestReleaseUrl = "https://github.com/boramee/jieip-manager/releases/latest";
    private const string ReleaseDownloadBase = "https://github.com/boramee/jieip-manager/releases/download";
    private const string AssetName = "JieipManager-win-x64.exe";

    public static Version CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

    public static async Task<ReleaseInfo> GetLatestReleaseAsync()
    {
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"JieipManager/{CurrentVersion}");
        using var response = await http.GetAsync(LatestReleaseUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var finalUri = response.RequestMessage?.RequestUri ?? throw new InvalidOperationException("최신 Release 주소를 확인할 수 없습니다.");
        var tag = finalUri.Segments.LastOrDefault()?.Trim('/') ?? "";
        var versionText = Uri.UnescapeDataString(tag).Trim().TrimStart('v', 'V');
        if (!Version.TryParse(versionText, out var version)) throw new InvalidOperationException($"알 수 없는 최신 버전 주소입니다: {finalUri}");
        var download = $"{ReleaseDownloadBase}/v{version}/{AssetName}";
        return new ReleaseInfo(version, download, "화면 및 기능 안정화 업데이트");
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

}
