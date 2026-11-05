using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TamaPoke.Utils
{
    public class AppVersionInfo
    {
        public string Version { get; set; } = "1.0.0.0";
        public string DownloadUrl { get; set; } = string.Empty;
    }

    // 버전 확인 결과: 서버 응답을 제대로 받았는지와, 새 버전 정보(없으면 null)
    public readonly record struct UpdateCheckResult(bool Succeeded, AppVersionInfo? NewVersion);

    public class AppVersionUpdater
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string SERVER_VERSION_URL = "https://raw.githubusercontent.com/gomdolit-jeong/TamaPoke_PC/main/version.json";

        public static string GetLocalVersion()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var versionInfo = JsonSerializer.Deserialize<AppVersionInfo>(json);
                    return versionInfo?.Version ?? "1.0.0.0";
                }
            }
            catch { }
            return "1.0.0.0";
        }

        // 실패(연결 오류, 잘못된 응답)를 "최신 버전"과 구분해서 돌려줍니다.
        public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(SERVER_VERSION_URL);
                if (response.IsSuccessStatusCode)
                    return ParseServerVersion(await response.Content.ReadAsStringAsync(), GetLocalVersion());
            }
            catch { }
            return new UpdateCheckResult(false, null);
        }

        public static UpdateCheckResult ParseServerVersion(string json, string localVersion)
        {
            try
            {
                var serverInfo = JsonSerializer.Deserialize<AppVersionInfo>(json);
                if (serverInfo == null || string.IsNullOrEmpty(serverInfo.Version)) return new UpdateCheckResult(false, null);
                bool isNewer = new Version(serverInfo.Version) > new Version(localVersion);
                return new UpdateCheckResult(true, isNewer ? serverInfo : null);
            }
            catch
            {
                return new UpdateCheckResult(false, null);
            }
        }

        // 새 버전 안내: 같은 버전은 한 번만 알립니다. (기록은 설정 파일과 따로 둬서 설정 저장에 덮어써지지 않게)
        private static string NoticePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update_notice.txt");

        // 처음 보는 버전이면 기록하고 true
        public static bool TryMarkNotified(string version)
        {
            try
            {
                if (File.Exists(NoticePath) && File.ReadAllText(NoticePath).Trim() == version) return false;
                File.WriteAllText(NoticePath, version);
            }
            catch { }
            return true;
        }

        // 안내할 새 버전이 있으면 그 정보를 돌려줍니다. (인터넷이 안 되면 null)
        public static async Task<AppVersionInfo?> CheckForNewVersionToNotifyAsync()
        {
            var info = (await CheckForUpdatesAsync()).NewVersion;
            return info != null && TryMarkNotified(info.Version) ? info : null;
        }

        // ==========================================
        // 🌟 싹 가져온 다운로드 및 배치 파일 생성 로직 (백그라운드 전담)
        // ==========================================
        public static async Task<bool> DownloadAndPrepareUpdateAsync(AppVersionInfo updateInfo, IProgress<string>? progress)
        {
            if (string.IsNullOrEmpty(updateInfo.DownloadUrl)) return false;

            string tempZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.zip");
            string extractPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_UpdateTemp");

            try
            {
                progress?.Report(Loc.T("update.app.downloading"));

                using (var response = await _httpClient.GetAsync(updateInfo.DownloadUrl))
                {
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(tempZipPath, FileMode.Create))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                progress?.Report(Loc.T("update.app.downloaded"));

                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                ZipFile.ExtractToDirectory(tempZipPath, extractPath);

                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);

                progress?.Report(Loc.T("update.app.ready"));
                return true;
            }
            catch (Exception ex)
            {
                progress?.Report(Loc.F("update.app.error", ex.Message));
                return false;
            }
        }

        // ==========================================
        // 🌟 프로그램 종료 시 덮어쓰기를 실행하는 배치 파일 마법 실행 메서드
        // ==========================================
        public static void ExecutePostUpdateBatch()
        {
            string batPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.bat");
            string exeName = Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName ?? "TamaPoke.exe");

            string batContent = $@"
@echo off
timeout /t 2 /nobreak > nul
xcopy /s /y /e ""_UpdateTemp\*"" "".\""
rmdir /s /q ""_UpdateTemp""
start """" ""{exeName}""
del ""%~f0""
";
            File.WriteAllText(batPath, batContent, Encoding.Default);

            Process.Start(new ProcessStartInfo()
            {
                FileName = batPath,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            System.Windows.Application.Current.Shutdown();
        }
    }
}