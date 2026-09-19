using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TamaPoke.Models;

namespace TamaPoke.Utils
{
    public class PokemonSpriteUpdater
    {
        // 🌟 100초(기본값) 대신 10분(600초)으로 넉넉하게 설정하여, 느린 환경에서도 끊기지 않되 영원히 멈추지는 않게 방어합니다.
        private static readonly HttpClient client = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        private const string RepoZipUrl = "https://github.com/PMDCollab/SpriteCollab/archive/refs/heads/master.zip";

        public static async Task DownloadAndConvertSpritesAsync(IProgress<string>? progress = null)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string targetResourceDir = Path.Combine(baseDir, "Assets", "Resource", "PokemonSprites");
            string dataFolderPath = Path.Combine(baseDir, "Data");

            string tempZipPath = Path.Combine(baseDir, "SpriteCollab_Temp.zip");
            string extractPath = Path.Combine(baseDir, "SpriteCollab_Extract");

            if (!Directory.Exists(targetResourceDir)) Directory.CreateDirectory(targetResourceDir);
            if (!Directory.Exists(dataFolderPath)) Directory.CreateDirectory(dataFolderPath);

            try
            {
                progress?.Report("[4단계] 대용량 포켓몬 스프라이트 원본(ZIP) 다운로드 시작...");
                progress?.Report("안내: 원본 파일이 커서 네트워크 환경에 따라 1~5분 정도 소요될 수 있습니다.");

                // 🌟 서버 통신 시작 (전체를 기다리지 않고 응답 헤더만 오면 바로 통과)
                using HttpResponseMessage response = await client.GetAsync(RepoZipUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                progress?.Report("✅ 서버 연결 성공! 실시간 다운로드를 시작합니다...");

                // 파일 전체 크기 파악 (서버에서 크기 정보를 제공할 경우)
                long? totalBytes = response.Content.Headers.ContentLength;

                // 🌟 데이터를 한 번에 받지 않고 스트림으로 조금씩 나눠서 받으며 진행률을 표시합니다.
                using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                using (FileStream fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    byte[] buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;
                    int reportCounter = 0;

                    // 데이터를 조각 단위로 계속 읽어옵니다.
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;
                        reportCounter++;

                        // 너무 자주 UI를 업데이트하면 화면이 끊기므로 약 1MB 다운로드 시점마다 진행률 표시
                        if (reportCounter % 128 == 0)
                        {
                            long downloadedMB = totalRead / 1024 / 1024;
                            if (totalBytes.HasValue)
                            {
                                long totalMB = totalBytes.Value / 1024 / 1024;
                                progress?.Report($"⬇️ 다운로드 진행 중... {downloadedMB} MB / {totalMB} MB");
                            }
                            else
                            {
                                progress?.Report($"⬇️ 다운로드 진행 중... {downloadedMB} MB 완료");
                            }
                        }
                    }
                }

                progress?.Report("✅ 다운로드 완료! 압축을 해제합니다. 잠시만 기다려주세요...");

                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                Directory.CreateDirectory(extractPath);

                ZipFile.ExtractToDirectory(tempZipPath, extractPath, true);

                string extractedMasterPath = Path.Combine(extractPath, "SpriteCollab-master");
                string sourceSpriteFolder = Path.Combine(extractedMasterPath, "sprite");
                string sourceTrackerPath = Path.Combine(extractedMasterPath, "tracker.json");
                string targetTrackerPath = Path.Combine(targetResourceDir, "tracker.json");

                if (File.Exists(sourceTrackerPath))
                {
                    File.Copy(sourceTrackerPath, targetTrackerPath, true);
                }

                progress?.Report("스프라이트 누락 번호를 검사 중입니다...");
                List<int> missingIds = new List<int>();

                for (int i = 1; i <= GameConstants.MAX_POKEMON_ID; i++)
                {
                    string folderName = i.ToString("D4");
                    string checkPath = Path.Combine(sourceSpriteFolder, folderName);

                    if (!Directory.Exists(checkPath))
                    {
                        missingIds.Add(i);
                    }
                }

                string missingJsonPath = Path.Combine(dataFolderPath, "missing_sprites.json");
                File.WriteAllText(missingJsonPath, JsonSerializer.Serialize(missingIds, new JsonSerializerOptions { WriteIndented = true }));

                progress?.Report($"[안내] 총 {missingIds.Count}마리의 스프라이트가 없어 알 부화 목록에서 제외됩니다.");
                progress?.Report("기존 SpriteConverter를 사용해 .bin 파일로 굽기 시작합니다...");

                string convertResult = SpriteConverter.BatchConvertAll(sourceSpriteFolder, targetResourceDir, targetTrackerPath);
                progress?.Report(convertResult);
            }
            catch (TaskCanceledException)
            {
                progress?.Report("❌ 다운로드 실패: 10분이 초과되어 통신이 강제 종료되었습니다. 네트워크 상태를 확인해 주세요.");
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ 스프라이트 다운로드/변환 중 오류 발생: {ex.Message}");
            }
            finally
            {
                progress?.Report("임시 대용량 다운로드 파일들을 청소합니다...");
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);

                progress?.Report("✨ 누락된 포켓몬 검사 및 .bin 굽기 작업이 완벽하게 끝났습니다!");
            }
        }
    }
}