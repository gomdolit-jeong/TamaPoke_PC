using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression; // 🌟 압축 해제를 위해 추가된 네임스페이스
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace TamaPoke.Utils
{
    public class AppVersionUpdater
    {
        private static readonly HttpClient client = new HttpClient();

        // 🌟 유저님의 실제 GitHub 저장소 Raw 주소
        private const string VersionCheckUrl = "https://raw.githubusercontent.com/gomdolit-jeong/TamaPoke_PC/main/version.json";

        // 서버에서 받아올 버전 정보 클래스
        private class VersionInfo
        {
            public string Version { get; set; } = "1.0.0.0";
            public string DownloadUrl { get; set; } = "";
        }

        // 🌟 업데이트가 필요하여 재시작을 해야 한다면 true를 반환합니다.
        public static async Task<bool> CheckAndUpdateProgramAsync(IProgress<string>? progress = null)
        {
            progress?.Report("🚀 [3단계] 프로그램 최신 버전을 확인합니다...");

            try
            {
                // 1. 현재 내 프로그램의 버전 확인
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

                // 2. 서버에서 최신 버전 정보 가져오기
                var response = await client.GetAsync(VersionCheckUrl);
                if (!response.IsSuccessStatusCode)
                {
                    progress?.Report("안내: 버전 서버를 찾을 수 없어 프로그램 업데이트를 건너뜁니다.");
                    return false;
                }

                string json = await response.Content.ReadAsStringAsync();
                var latestInfo = JsonSerializer.Deserialize<VersionInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (latestInfo == null || string.IsNullOrEmpty(latestInfo.DownloadUrl)) return false;

                Version latestVersion = new Version(latestInfo.Version);

                // 3. 최신 버전이 더 높다면 업데이트 진행!
                if (latestVersion > currentVersion)
                {
                    progress?.Report($"🎉 새로운 버전({latestVersion})이 발견되었습니다! 압축 파일 다운로드를 시작합니다...");

                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string zipFilePath = Path.Combine(baseDir, "TamaPoke_Update.zip");
                    string extractPath = Path.Combine(baseDir, "UpdateTemp");

                    // 기존에 남아있을지 모르는 찌꺼기 파일/폴더 정리
                    if (File.Exists(zipFilePath)) File.Delete(zipFilePath);
                    if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);

                    // 🌟 [변경점 1] ZIP 파일 다운로드
                    byte[] fileBytes = await client.GetByteArrayAsync(latestInfo.DownloadUrl);
                    await File.WriteAllBytesAsync(zipFilePath, fileBytes);
                    progress?.Report("다운로드 완료! 압축을 해제하는 중입니다...");

                    // 🌟 [변경점 2] 다운로드한 ZIP 파일 압축 해제
                    Directory.CreateDirectory(extractPath);
                    ZipFile.ExtractToDirectory(zipFilePath, extractPath, true); // true: 덮어쓰기 허용

                    progress?.Report("압축 해제 완료! 업데이트를 적용하기 위해 프로그램을 재시작합니다.");

                    // 🌟 [변경점 3] 폴더 전체 복사 및 정리를 위한 배치 파일(.bat) 생성
                    string batPath = Path.Combine(baseDir, "update_apply.bat");

                    string batScript = $@"
@echo off
:: 프로그램이 완전히 종료될 시간을 벌어줍니다 (2초)
timeout /t 2 /nobreak >nul

:: UpdateTemp 폴더 안의 모든 파일과 하위 폴더를 현재 디렉토리로 덮어쓰기 복사합니다.
xcopy ""UpdateTemp\*"" "".\*"" /s /e /y /q

:: 복사가 끝났으니 임시 폴더와 ZIP 파일을 삭제하여 깔끔하게 청소합니다.
rmdir /s /q ""UpdateTemp""
del ""TamaPoke_Update.zip""

:: 다마포케를 다시 실행합니다.
start """" ""TamaPoke.exe""

:: 마지막으로 이 배치 파일 자신을 삭제합니다.
del ""%~f0""
";
                    File.WriteAllText(batPath, batScript);

                    // 5. 배치 파일 실행
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = batPath,
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    Process.Start(psi);

                    // 덮어쓰기를 위해 현재 프로그램 강제 종료 알림
                    return true;
                }
                else
                {
                    progress?.Report($"현재 최신 버전(v{currentVersion})의 다마포케를 사용 중입니다.");
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"[안내] 프로그램 버전 확인을 건너뜁니다: {ex.Message}");
            }

            return false;
        }
    }
}