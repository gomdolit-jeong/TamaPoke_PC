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
            string targetResourceDir = AppPaths.SpriteFolder;
            string dataFolderPath = AppPaths.DataFolder;

            string tempZipPath = Path.Combine(baseDir, "SpriteCollab_Temp.zip");
            string extractPath = Path.Combine(baseDir, "SpriteCollab_Extract");

            if (!Directory.Exists(targetResourceDir)) Directory.CreateDirectory(targetResourceDir);
            if (!Directory.Exists(dataFolderPath)) Directory.CreateDirectory(dataFolderPath);

            try
            {
                progress?.Report(Loc.T("update.sprite.start"));
                progress?.Report(Loc.T("update.sprite.slow"));

                // 🌟 서버 통신 시작 (전체를 기다리지 않고 응답 헤더만 오면 바로 통과)
                using HttpResponseMessage response = await client.GetAsync(RepoZipUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                progress?.Report(Loc.T("update.sprite.connected"));

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
                                progress?.Report(Loc.F("update.sprite.progress", downloadedMB, totalMB));
                            }
                            else
                            {
                                progress?.Report(Loc.F("update.sprite.progressNoTotal", downloadedMB));
                            }
                        }
                    }
                }

                progress?.Report(Loc.T("update.sprite.unzipping"));

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

                progress?.Report(Loc.T("update.sprite.scanning"));
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

                progress?.Report(Loc.F("update.sprite.missing", missingIds.Count));
                progress?.Report(Loc.T("update.sprite.converting"));

                string convertResult = SpriteConverter.BatchConvertAll(sourceSpriteFolder, targetResourceDir, targetTrackerPath);
                BinSpriteReader.ClearCache(); // 새로 만든 bin(재생시간 포함)을 다시 읽도록 캐시 초기화
                progress?.Report(convertResult);
            }
            catch (TaskCanceledException)
            {
                progress?.Report(Loc.T("update.sprite.timeout"));
            }
            catch (Exception ex)
            {
                progress?.Report(Loc.F("update.sprite.error", ex.Message));
            }
            finally
            {
                progress?.Report(Loc.T("update.sprite.cleanup"));
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);

                progress?.Report(Loc.T("update.sprite.done"));
            }
        }
    }
}