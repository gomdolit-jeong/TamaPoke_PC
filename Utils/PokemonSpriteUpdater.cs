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
        private static readonly HttpClient client = new HttpClient()
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        // PMDCollab 전체를 한 번에 받을 수 있는 GitHub 공식 ZIP 다운로드 주소입니다.
        private const string RepoZipUrl = "https://github.com/PMDCollab/SpriteCollab/archive/refs/heads/master.zip";

        public static async Task DownloadAndConvertSpritesAsync(IProgress<string>? progress = null)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string targetResourceDir = Path.Combine(baseDir, "Assets", "Resource", "PokemonSprites");
            string dataFolderPath = Path.Combine(baseDir, "Data");

            // 대용량 처리를 위한 임시 경로 설정
            string tempZipPath = Path.Combine(baseDir, "SpriteCollab_Temp.zip");
            string extractPath = Path.Combine(baseDir, "SpriteCollab_Extract");

            if (!Directory.Exists(targetResourceDir)) Directory.CreateDirectory(targetResourceDir);
            if (!Directory.Exists(dataFolderPath)) Directory.CreateDirectory(dataFolderPath);

            try
            {
                progress?.Report("[4단계] 대용량 포켓몬 스프라이트 원본(ZIP) 다운로드 시작...");
                progress?.Report("안내: 원본 파일이 커서 네트워크 환경에 따라 1~5분 정도 소요될 수 있습니다.");

                // 1. 안전하게 원본 ZIP 통째로 다운로드
                using HttpResponseMessage response = await client.GetAsync(RepoZipUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode(); // 주소가 틀렸거나 서버 에러(404 등)가 나면 즉시 예외를 발생시킵니다.

                // 연결이 성공했음을 UI에 즉시 알립니다.
                progress?.Report("✅ 서버 연결 성공! 대용량 파일을 디스크에 내려받고 있습니다...");

                // 메모리에 한 번에 올리지 않고, 조금씩 디스크(파일)로 바로 씁니다.
                using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                using (FileStream fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    await contentStream.CopyToAsync(fileStream);
                }

                progress?.Report("다운로드 완료! 압축을 해제합니다. 잠시만 기다려주세요...");

                // 2. 압축 해제 전 찌꺼기 폴더 초기화
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                Directory.CreateDirectory(extractPath);

                // 3. 압축 해제 (시간이 조금 걸리는 작업입니다)
                ZipFile.ExtractToDirectory(tempZipPath, extractPath, true);

                // 4. 압축 풀린 폴더 내부의 경로 찾기
                string extractedMasterPath = Path.Combine(extractPath, "SpriteCollab-master");
                string sourceSpriteFolder = Path.Combine(extractedMasterPath, "sprite");
                string sourceTrackerPath = Path.Combine(extractedMasterPath, "tracker.json");
                string targetTrackerPath = Path.Combine(targetResourceDir, "tracker.json");

                // 나중을 위해 tracker.json 파일을 타겟 폴더로 복사해 둡니다.
                if (File.Exists(sourceTrackerPath))
                {
                    File.Copy(sourceTrackerPath, targetTrackerPath, true);
                }

                // ==========================================
                // 🌟 파이썬 스크립트 C# 완벽 이식: 누락된 번호 찾기
                // ==========================================
                progress?.Report("스프라이트 누락 번호를 검사 중입니다...");
                List<int> missingIds = new List<int>();

                for (int i = 1; i <= GameConstants.MAX_POKEMON_ID; i++)
                {
                    string folderName = i.ToString("D4"); // 예: "0001"
                    string checkPath = Path.Combine(sourceSpriteFolder, folderName);

                    // 폴더가 존재하지 않으면 결측치 목록에 추가!
                    if (!Directory.Exists(checkPath))
                    {
                        missingIds.Add(i);
                    }
                }

                // 결과를 Data 폴더에 missing_sprites.json 으로 저장 (알 부화 방지용)
                string missingJsonPath = Path.Combine(dataFolderPath, "missing_sprites.json");
                File.WriteAllText(missingJsonPath, JsonSerializer.Serialize(missingIds, new JsonSerializerOptions { WriteIndented = true }));

                progress?.Report($"[안내] 총 {missingIds.Count}마리의 스프라이트가 없어 알 부화 목록에서 제외됩니다.");
                progress?.Report("기존 SpriteConverter를 사용해 .bin 파일로 굽기 시작합니다...");

                // ==========================================
                // 5. 유저님의 기존 SpriteConverter 클래스 호출
                // ==========================================
                // 임시 원본 폴더의 데이터를 읽어, 타겟 경로에 .bin 파일들을 생성합니다.
                string convertResult = SpriteConverter.BatchConvertAll(sourceSpriteFolder, targetResourceDir, targetTrackerPath);

                // 결과 로그 (성공/스킵/실패 개수) 출력
                progress?.Report(convertResult);
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ 스프라이트 다운로드/변환 중 오류 발생: {ex.Message}");
            }
            finally
            {
                // 6. 용량이 엄청난 임시 원본 파일들을 깨끗하게 청소합니다!
                progress?.Report("임시 대용량 다운로드 파일들을 청소합니다...");
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);

                progress?.Report("✨ 누락된 포켓몬 검사 및 .bin 굽기 작업이 완벽하게 끝났습니다!");
            }


        }
    }
}