using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TamaPoke.Utils
{
    public class AppVersionInfo
    {
        public string Version { get; set; } = "1.0.0.0";
        public string DownloadUrl { get; set; } = string.Empty;
    }

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

        public static async Task<AppVersionInfo?> CheckForUpdatesAsync()
        {
            try
            {
                string localVersionString = GetLocalVersion();
                Version localVersion = new Version(localVersionString);

                var response = await _httpClient.GetAsync(SERVER_VERSION_URL);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var serverInfo = JsonSerializer.Deserialize<AppVersionInfo>(json);

                    if (serverInfo != null && !string.IsNullOrEmpty(serverInfo.Version))
                    {
                        Version serverVersion = new Version(serverInfo.Version);
                        if (serverVersion > localVersion) return serverInfo;
                    }
                }
            }
            catch { }
            return null;
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
                progress?.Report("- 업데이트 파일을 다운로드하는 중입니다... 잠시만 기다려주세요.");

                using (var response = await _httpClient.GetAsync(updateInfo.DownloadUrl))
                {
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(tempZipPath, FileMode.Create))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                progress?.Report("- 다운로드 완료! 압축을 풀고 적용을 준비합니다...");

                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                ZipFile.ExtractToDirectory(tempZipPath, extractPath);

                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);

                progress?.Report("- 업데이트 준비 완료! 창을 닫으면 자동으로 버전이 교체됩니다.");
                return true;
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ 다운로드 또는 압축 해제 중 오류 발생: {ex.Message}");
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