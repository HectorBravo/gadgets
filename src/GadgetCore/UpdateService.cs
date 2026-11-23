using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

namespace HectorBravo.Gadgets.Core;

/// <summary>
/// GitHub Releases update notifier (pattern ported from nvidia_app's
/// UpdateService). Tags look like "&lt;gadget&gt;/&lt;yyyyMMdd_HHmm&gt;"; a release
/// published after our exe was built means "update available". Applying one:
/// download zip → verify → PowerShell swap-helper waits for exit, backs up,
/// extracts over the app dir, relaunches.
/// </summary>
public sealed class UpdateService
{
    private const string RepoApi = "https://api.github.com/repos/HectorBravo/gadgets";
    private static readonly HttpClient Http = CreateHttp();

    private readonly string _gadget;   // e.g. "cpu-meter"
    private readonly string _exeName;  // e.g. "CpuMeter.exe"

    public UpdateService(string gadget, string exeName)
    {
        _gadget = gadget;
        _exeName = exeName;
    }

    public sealed record UpdateInfo(DateTime PublishedUtc, string DownloadUrl, string Tag);

    private static HttpClient CreateHttp()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("RustGadgets-updater");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        var token = GitHubToken();
        if (!string.IsNullOrWhiteSpace(token))
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return http;
    }

    private static string? _cachedToken;
    private static string? GitHubToken()
    {
        if (_cachedToken != null) return _cachedToken == "" ? null : _cachedToken;
        try
        {
            foreach (var name in new[] { "GITHUB_TOKEN", "GH_TOKEN" })
            {
                var t = Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrWhiteSpace(t)) { _cachedToken = t.Trim(); return _cachedToken; }
            }
            var psi = new ProcessStartInfo("gh", "auth token")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p != null && p.WaitForExit(3000) && p.ExitCode == 0)
            {
                var t = p.StandardOutput.ReadToEnd().Trim();
                if (!string.IsNullOrWhiteSpace(t)) { _cachedToken = t; return _cachedToken; }
            }
        }
        catch { /* stay anonymous */ }
        _cachedToken = "";
        return null;
    }

    /// <summary>Build stamp = LastWriteTime of the running exe (release.ps1 stamps it).</summary>
    public static DateTime BuildStamp =>
        new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).LastWriteTime;

    /// <summary>Newest release for THIS gadget published after our build, else null.</summary>
    public async Task<UpdateInfo?> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            using var resp = await Http.GetAsync($"{RepoApi}/releases?per_page=100", ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return null;

            await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;

            DateTime? bestPublished = null;
            string? bestTag = null, bestUrl = null;

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (!el.TryGetProperty("tag_name", out var tagEl)) continue;
                var tag = tagEl.GetString();
                if (tag == null || !tag.StartsWith(_gadget + "/", StringComparison.Ordinal)) continue;

                DateTime published = DateTime.MinValue;
                if (!el.TryGetProperty("published_at", out var pa) || pa.ValueKind != JsonValueKind.String)
                    el.TryGetProperty("created_at", out pa);
                if (pa.ValueKind == JsonValueKind.String &&
                    DateTimeOffset.TryParse(pa.GetString(), out var parsed))
                    published = parsed.UtcDateTime;

                if ((bestPublished == null || published > bestPublished) &&
                    el.TryGetProperty("assets", out var assets) &&
                    assets.ValueKind == JsonValueKind.Array && assets.GetArrayLength() > 0 &&
                    assets[0].TryGetProperty("browser_download_url", out var dl) &&
                    dl.ValueKind == JsonValueKind.String)
                {
                    bestPublished = published;
                    bestTag = tag;
                    bestUrl = dl.GetString();
                }
            }

            if (bestPublished == null || bestUrl == null) return null;
            // only offer updates published after this exe was built
            if (bestPublished.Value <= BuildStamp.ToUniversalTime()) return null;

            return new UpdateInfo(bestPublished.Value, bestUrl, bestTag!);
        }
        catch (OperationCanceledException) { throw; }
        catch { return null; } // never disturb the gadget over a failed check
    }

    /// <summary>
    /// Downloads + verifies the zip, stages the swap helper and shuts the app down.
    /// Throws before shutdown on any failure.
    /// </summary>
    public async Task DownloadAndApplyAsync(UpdateInfo info, CancellationToken ct = default)
    {
        var tag = info.Tag.Replace('/', '_');
        var temp = Path.GetTempPath();
        var zipPath = Path.Combine(temp, $"{_gadget}-update-{tag}.zip");
        var scriptPath = Path.Combine(temp, $"{_gadget}-update.ps1");

        using (var resp = await Http.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
        {
            resp.EnsureSuccessStatusCode();
            await using var src = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var dst = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await src.CopyToAsync(dst, ct).ConfigureAwait(false);
        }

        // verify the zip contains our exe before touching anything
        using (var zip = ZipFile.OpenRead(zipPath))
        {
            bool hasExe = false;
            foreach (var entry in zip.Entries)
            {
                var name = entry.FullName;
                if (name.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(name) || name.Contains(':'))
                    throw new InvalidDataException($"Unsafe entry in update zip: {name}");
                if (string.Equals(Path.GetFileName(name), _exeName, StringComparison.OrdinalIgnoreCase))
                    hasExe = true;
            }
            if (!hasExe) throw new InvalidDataException($"Update zip does not contain {_exeName}.");
        }

        var appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
        var backupDir = Path.Combine(temp, $"{_gadget}-backup-{tag}");
        var pid = Environment.ProcessId;

        static string Esc(string s) => s.Replace("'", "''");
        var script =
            "$ErrorActionPreference='Stop'\n" +
            $"$appPid={pid}\n" +
            $"$zip='{Esc(zipPath)}'\n" +
            $"$appDir='{Esc(appDir)}'\n" +
            $"$backup='{Esc(backupDir)}'\n" +
            "$deadline=(Get-Date).AddSeconds(60)\n" +
            "while((Get-Date) -lt $deadline){ if(-not (Get-Process -Id $appPid -ErrorAction SilentlyContinue)){break}; Start-Sleep -Milliseconds 500 }\n" +
            "Start-Sleep -Milliseconds 1000\n" +
            "New-Item -ItemType Directory -Force -Path $backup | Out-Null\n" +
            "Get-ChildItem -LiteralPath $appDir -Include *.exe,*.dll,*.json -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $backup -Force }\n" +
            "Expand-Archive -LiteralPath $zip -DestinationPath $appDir -Force\n" +
            $"Start-Process -FilePath (Join-Path $appDir '{_exeName}') -WorkingDirectory $appDir\n" +
            $"Remove-Item -LiteralPath '{Esc(scriptPath)}' -ErrorAction SilentlyContinue\n";
        await File.WriteAllTextAsync(scriptPath, script, ct).ConfigureAwait(false);

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c powershell -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        if (Process.Start(psi) == null)
            throw new InvalidOperationException("Could not start the update helper.");

        System.Windows.Application.Current?.Dispatcher.Invoke(
            () => System.Windows.Application.Current.Shutdown());
    }
}
